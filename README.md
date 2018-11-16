# DDCKVM
A windows service to automatically switch monitors on USB events. Combine with a cheap USB switch to have a full software emulation of an often pricy KVM switch.

## Setup

1) Download a release
2) Move the extracted folder to *Program Files* or somewhere else safe (don't worry about permissions, the service runs as SYSTEM)
3) Run `Install.bat`
4) Follow the on-screen instructions in your browser.

To uninstall, run `Uninstall.bat`, then optionally remove the folder

The service will automatically be started on windows startup.

## Configuration

Configuration can be accessed at http://localhost:4280 (**only on the machine the service is running**). This site will open automatically on service installation.

For manual configuration, edit the file `ddc_config.json` in the installation folder.
