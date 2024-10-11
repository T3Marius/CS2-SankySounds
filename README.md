# Sanky-Sounds

Basically what this plugin does is that you can play a song for everyone by just typing a custom word to chat, with special permission too.

To use the sounds you need to have the following plugin: https://github.com/Source2ZE/MultiAddonManager and add the addons in the cfg file.

You need to upload your sounds via workshop or use the existing workshop custom sounds.

You can take the workshop id and search it on /steamapps/workshop/content/730. (your id should be something like this: "302931942")

After that download the Source2Viewer: https://valveresourceformat.github.io/

Drag&Drop the .vpk file from the workshop id in Source2Viewer, take the file path and then put it in config as i show you:

```
{
[Sounds]
sound1 = "sounds/sankysounds/sound1"
sound2 = "sounds/sankysounds/sound2"
sound3 = "sounds/sankysounds/sound3"

[Permissions]
Permissions = ["@css/root", "@css/vips"]

[Settings]
CommandsCooldown = 15
SoundsPrefix = "."
EnableMenu = true
SankyMenu = [ "sankysounds" ]
}
```
Then after you type example to chat it will play the sound. you don't need any prefix, just "example"

If you don't want all the players on server to use the sounds, change permission flag in the config.
