# Launcher

Think of this as a self-contained version of Rainmeter gadget designed just for launching stuff.
This is way faster and non-intrusive than Rainmeter when added to `PATH`.

* Open exe with arguments
* Open file/folder location
* Open website

## TODO

- [ ] Support comments on link items (not urgent).
- [ ] Support in-place arguments on link items (aka. so we don't always need to take from CLI use) (not urgent); This is non-trivial architectural change and may complicate code.
- [ ] (CLI, GUI) Add simple `--interactive/-i` mode which allows picking targets interactively. Occassionally useful when we are lazy. It should be a TUI-like interface.

## Version Changelog

* v0.1.0: Functional release.
* v0.1.1: Add `--print`.
* v0.1.2: Add `--edit` as alias to `--config`; Provides `-` type shortcuts to long `--` type command names.
* v0.1.3: Add `--open`.
* v0.1.4: Surpport verbatim paths.
* v0.1.5: Add `--create` command; Remove `--config`, use `--edit` instead.