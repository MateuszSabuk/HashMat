# HashMat
Web App with graphical interface for hash related tools.

## Implementation
This is an ASP.NET Core application built on docker.

The project was written in C# and js using Visual Studio.


## Installation
**Docker must be installed for the app to run**

There are two ways to install the app

If you want to edit or contribute:

1. Clone this project and open it using Visual Studio

or if you just want to run it:

1. Pull the docker image from [docker hub](https://hub.docker.com/r/samatebu/hashmat)
2. Run the container with port 80 translated to a host port 


## TODOs
- Change the time in the base container
- John implementation
  - Process sessions
    - Add session names
    - Tracking running sessions
    - Stopping and resuming the sessions
  - Saving uploaded wordlists
  - Incremental mode rules
- Hashcat
