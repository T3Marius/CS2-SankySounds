# Sanky-Sounds
Basically what this plugin does is that 

To use the sounds you need to have the following plugin: https://github.com/Source2ZE/MultiAddonManager

You need to upload your sounds via workshop or use the existing workshop custom sounds

After that , download the Source2Viewer: https://valveresourceformat.github.io/

After your done download it, take the workshop id and search it on /steamapps/workshop/content/730. (your id should be something like this: "302931942")

Drag&Drop the file .vpk file that pops up after you search it in Source2Viewer, take the file path and then put it in config as i show you:
{
  "Sounds": {
   "ownage": "sounds/therazu/ownage.vsnd"
  },
  "ConfigVersion": 1
}
Then after you type ownage to chat it will play the sound. you don t need any prefix, just "ownage"

to add more sounds just put a "," after the "sounds/therazu/ownage.vsnd" and contiune.

You need to add a permission flag too, if you don t want all the players on server to use the sounds.
