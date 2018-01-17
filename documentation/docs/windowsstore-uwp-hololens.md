# Exporting to Windows Store (UWP, HoloLens)

This SDK has a dependency on `sqlite` which has to be installed seperately after building the `Windows Store` solution:

* Open exported `Windows Store` solution
* `Tools -> Extensions and Updates`
  * Search for `sqlite`
  * Install `SQLite for Universal Windows Platform`
* Right click on `References` of the default project
  * `Add Reference`
  * Expand `Universal Windows` on the left
  * Click `Extensions`
  * Check `SQLite for Universal Windows Platform`

The `Windows Store` solution should build successfully now.