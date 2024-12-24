# verify and assemble.py
Auto assemble world downloads, and verify everything is in a reset state.
Reset includes:
* Remove command storage
* Remove scoreboard/team data
* Remove custom boss bars
* Remove player/statistic/advancement progress data
* Remove forceloaded chunks
* Remove POI data
* Remove entities
* Remove enabled data packs, with the exception of `file/MEDACORP`
* Set sendCommandFeedback to false
* Rename `Official add-on/*/*.mcmeta` files back to `pack.mcmeta`
* Move any generated structure files to the MEDACORP data pack

Build will save a copmpressed copy of the root directory to `<WORLDNAME> (v<number>).zip`, except `.git/` and files included in the folder's `.gitignore`

# euler to transformation rotation.py
Converts Euler angles to Axis-Angle or Quaternion rotations.
This is to be replaced with the Animator program below.

# Animator
## WIP
Program for easily creating animations.
Implemented:
* Automatic world/data pack detection
* Automatic animation group (entity) detection
* Automatic animation file detection
* Selecting (Composite) model files per part (variant)
* Saving selected models to file so that you're not asked every time you select that animation group
* Models get data from parent (and parent's parent's, and parent's parent's parent's, and....)
* Rendering selected models (PARTIALLY; UV mapping rotation doesn't work yet) with offsets defined by animation group

To implement:
* Add checkboxes for tag toggling, using different offsets (eg `flipped_gravity`)
* Add checkbox for toggling between armor stand and item display
* - toggle built-in offset
* - toggle between just rotating, and all kinds of changes to the model transform
* Add option to change entity Y position compared to the block
* Allow selecting model part to rotate them around their display center
* Read animation files
* - Armor Stand variant (Pose)
* - Item Display variant (transformation)
* Play animation
* Output animation files
* - Armor Stand variant (Pose)
* - Item Display variant (transformation)