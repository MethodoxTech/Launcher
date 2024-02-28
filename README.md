# Launcher

THIS CODE IS CROSS-PLATFORM COMPLIANT IN THE SENSE THAT IT DOESN'T USE ANY WINDOWS SPECIFIC API; HOWEVER, IT UTILIZES explorer.exe TO OPEN DISK LOCATIONS WHICH IS ONLY APPLICABLE ON WINDOWS.

Think of this as a self-contained version of Rainmeter gadget designed just for launching stuff.
This is way faster and non-intrusive than Rainmeter when added to `PATH`.
You can also think of it as an extended version of "task bar".

* Open exe with arguments
* Open file/folder location
* Open website

## Setup

You need to make sure you have default text editor for .yaml file, otherwise you might encounter "Application Not Found" error on Windows.
For using with Visual Studio Code, you probably want to expose `%UserProfile%\AppData\Local\Programs\Microsoft VS Code` instead of `%UserProfile%\AppData\Local\Programs\Microsoft VS Code\bin` which is the default (the latter contains cmd scripts instead of actual executables which will be an issue when Launcher tries to launch the process).

## TODO

- [ ] Support comments on link items (not urgent).
- [ ] Support in-place arguments on link items (aka. so we don't always need to take from CLI use) (not urgent); This is non-trivial architectural change and may complicate code.
- [ ] (CLI, GUI) Add simple `--interactive/-i` mode which allows picking targets interactively. Occassionally useful when we are lazy. It should be a TUI-like interface.
- [ ] Add unit test to test parsing of various file paths

## Version Changelog

* v0.1.0: Functional release.
* v0.1.1: Add `--print`.
* v0.1.2: Add `--edit` as alias to `--config`; Provides `-` type shortcuts to long `--` type command names.
* v0.1.3: Add `--open`.
* v0.1.4: Surpport verbatim paths.
* v0.1.5: Add `--create` command; Remove `--config`, use `--edit` instead.
* v0.1.6: Fix issue with parsing folder paths with spaces when opening from explorer.

## Dated Notes

### 20240111

This is an inspirational quick-shot implementation of a specific idea - and it works very well! So far it's been very helpful and I can confirm that this kind of manual configuration based on workload (even though we could, we don't need to sync the configurations cross different work stations) is workload-specific, and easy to use.
