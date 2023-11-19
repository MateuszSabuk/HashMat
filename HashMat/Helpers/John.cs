using Microsoft.AspNetCore.SignalR;
using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace HashMat.Helpers
{
    public class John
    {
        private IFormFile hashListFile;
        private IFormFile wordListFile;
        private string input;
        private string algorithm;
        private string wordListOption;
        private string inputOption;
        private string selectedWordList;
        private string selectedEncoding;
        private bool incremental;

        public John(IFormFile hashListFile,
                    IFormFile wordListFile,
                    string input,
                    string algorithm,
                    string wordListOption,
                    string inputOption,
                    string selectedWordList,
                    string selectedEncoding,
                    bool incremental)
        {
            this.hashListFile = hashListFile;
            this.wordListFile = wordListFile;
            this.input = input;
            this.algorithm = algorithm;
            this.wordListOption = wordListOption;
            this.inputOption = inputOption;
            this.selectedWordList = selectedWordList;
            this.selectedEncoding = selectedEncoding;
            this.incremental = incremental;
        }

        public static List<string> GetAvailableAlgorithms()
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "/opt/john/run/john",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = "--list=formats"
            };

            string output = "";
            using (Process process = new Process { StartInfo = psi })
            {
                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        output += e.Data;
                    }
                };

                process.Start();
                process.BeginOutputReadLine();

                process.WaitForExit();
            }
            string formats = String.Concat(output.Where(c => !Char.IsWhiteSpace(c)));
            return ("Automatic,"+formats).Split(',').ToList();
        }

        public static List<string> GetAvailableWordlists()
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "ls",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = "/opt/wordlists"
            };

            string output = "";
            using (Process process = new Process { StartInfo = psi })
            {
                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        output += ","+ e.Data;
                    }
                };

                process.Start();
                process.BeginOutputReadLine();

                process.WaitForExit();
            }
            return output.Length>0?output[1..].Split(',').ToList(): new List<string>();
        }

        public string ValidateInput()
        {
            StringBuilder problemBuilder = new StringBuilder();

            // Validate wordListOption
            string[] allowedWordListOptions = { "no-wordlist", "file", "selection" };
            if (!allowedWordListOptions.Contains(wordListOption))
            {
                problemBuilder.AppendLine("Invalid Word List Option selected.");
            }

            // Validate inputOption
            string[] allowedInputOptions = { "input", "file" };
            if (!allowedInputOptions.Contains(inputOption))
            {
                problemBuilder.AppendLine("Invalid Input Option selected.");
            }

            // Validate encoding
            string[] allowedEncodingOptions = { "ascii", "utf8", "latin1" };
            if (!allowedEncodingOptions.Contains(selectedEncoding))
            {
                problemBuilder.AppendLine("Invalid Encoding Option selected.");
            }

            // Validate hash input
            if (inputOption == "input" && (input == null || input.Length == 0))
            {
                problemBuilder.AppendLine("Input is required when Hash Input Option is set to 'input'.");
            }

            if (inputOption == "file" && (hashListFile == null || hashListFile.Length == 0))
            {
                problemBuilder.AppendLine("Hash List File is required when Hash Input Option is set to 'file'.");
            }

            // Validate wordlist input
            if (wordListOption == "file" && (wordListFile == null || wordListFile.Length == 0))
            {
                problemBuilder.AppendLine("Word List File is required when Word List Option is set to 'file'.");
            }

            // TODO: Validate that the wordlist does exist

            // TODO: More validation (Definitely file and algorithm validations

            return problemBuilder.ToString();
        }
        public string CreateJohnCommand()
        {
            // Create list of arguments for john
            string command = string.Empty;
            if(incremental)
            {
                command += "--incremental ";
            } 
            else {

                // Set wordlist
                if (wordListOption == "no-wordlist")
                {
                }
                else if (wordListOption == "file")
                {
                    var filePath = $"/tmp/{Helper.GetReadableTimestamp()}_WordList.txt";

                    try
                    {
                        var hashListFileHash = Helper.GetHashOfFile(wordListFile);
                        var existingFilePath = Path.Combine("/tmp", $"{hashListFileHash}.txt");

                        command += File.Exists(existingFilePath) ? $"--wordlist={existingFilePath} " : $"--wordlist={filePath} ";

                        if (!File.Exists(existingFilePath))
                        {
                            // Copy the content of wordListFile directly to the file
                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                wordListFile.CopyTo(fileStream);
                            }

                            File.SetAttributes(filePath, FileAttributes.Hidden);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error writing word list to {filePath}: {ex.Message}");
                    }
                }
                else if (wordListOption == "selection")
                {
                    var wordListPath = $"/opt/wordlists/{selectedWordList}";

                    command += File.Exists(wordListPath) ? $"--wordlist={wordListPath} " : " ";

                    if (!File.Exists(wordListPath))
                    {
                        Console.Error.WriteLine("Error: No wordlist selected");
                    }
                }
            } // END of if not incremental 

            // Set format
            if (!string.IsNullOrEmpty(algorithm))
            {
                var format = algorithm == "Automatic" ? "all" : algorithm;
                command += $"--format={format} ";
            }

            // Set encoding
            Encoding chosenEncoding = Encoding.UTF8;
            if (selectedEncoding == "utf8")
            {
                command += "--encoding=UTF-8 ";
            } 
            else if (selectedEncoding == "ascii")
            {
                chosenEncoding = Encoding.ASCII;
                command += "--encoding=ASCII ";
            }
            else if (selectedEncoding == "latin1")
            {
                chosenEncoding = Encoding.Latin1;
                command += "--encoding=ISO-8859-1 ";
            }


            // input
            if (inputOption == "input")
            {
                // Safely write the input to a file
                const string fileName = "/tmp/hash.txt";

                try
                {
                    File.WriteAllText(fileName, input, chosenEncoding);
                    File.SetAttributes(fileName, FileAttributes.Hidden);
                    command += fileName + " ";
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error writing hash to {fileName}: {ex.Message}");
                }
            }
            else if (inputOption == "file")
            {
                var filePath = $"/tmp/{Helper.GetHashOfFile(hashListFile)}.txt";
                try
                {
                    var hashListFileHash = Helper.GetHashOfFile(hashListFile);
                    var existingFilePath = Path.Combine("/tmp", $"{hashListFileHash}.txt");

                    command += (File.Exists(existingFilePath) ? existingFilePath : filePath) + " ";

                    Console.WriteLine("File found?: "+ (File.Exists(existingFilePath) ? existingFilePath : filePath));
                    if (!File.Exists(existingFilePath))
                    {
                        // Read the content of hashListFile and write it to the file
                        using (var streamReader = new StreamReader(hashListFile.OpenReadStream()))
                        {
                            File.WriteAllText(filePath, streamReader.ReadToEnd(), chosenEncoding);
                            File.SetAttributes(filePath, FileAttributes.Hidden);
                            Console.WriteLine("FileWritten");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error writing hash list to {filePath}: {ex.Message}");
                }
            }
            Console.WriteLine("The return of the command");
            return command;
        }

    }
}
