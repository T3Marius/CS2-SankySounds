# Sanky-Sounds
Thank you, @exkludera for your help!

Basically what this plugin does is that you can play a song for everyone by just typing a custom word to chat, with special permission too.

To use the sounds you need to have the following plugin: https://github.com/Source2ZE/MultiAddonManager and add the addons in the cfg file.

You need to upload your sounds via workshop or use the existing workshop custom sounds.

You can take the workshop id and search it on /steamapps/workshop/content/730. (your id should be something like this: "302931942")

After that download the Source2Viewer: https://valveresourceformat.github.io/

Drag&Drop the .vpk file from the workshop id in Source2Viewer, take the file path and then put it in config as i show you:

```
{
  "Sounds": {
   "example": "sounds/example.vsnd"
  },
  "Permission": "@css/generic",
  "ConfigVersion": 1
}
```
Then after you type example to chat it will play the sound. you don't need any prefix, just "example"

To add more sounds just put a "," after the "sounds/example.vsnd" and contiune.

If you don't want all the players on server to use the sounds, change permission flag in the config.
