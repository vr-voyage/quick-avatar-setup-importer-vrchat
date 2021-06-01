# THIS SOFTWARE IS IN ALPHA STATE

# MAKE A BACKUP OF ANY AVATAR AND SCENE, YOU'RE USING THIS TOOL ON


About
--------

This tool allows you to import avatar setup done through the Quick Avatar Setup tool,
available here https://github.com/vr-voyage/quick-avatar-setup

At the moment, this only supports importing 'emotions', AKA blendshapes configurations,
as emotes that can be activated through the Expressions menus.  
The tool generate the animation controller, the animation, the menus and parameters
automatically, and set them up on the provided avatar.

Again, this software is in alpha state and is very likely to fail. 

**DO A BACKUP OF THE AVATARS YOU PASS TO THIS TOOL !**

Usage
--------

Put **QuickAvatarSetupImporter.cs** inside your Unity project where you're
configuring your avatars.  

Once done, the menu "Tools -> Quick Avatar Setup importer" should appear.

Copy the `.avatar_setup.json`  file inside your project, then open
the **Quick Avatar Setup importer**.

Once opened, set the avatar (with a VRC Avatar Descritpor attached) you
want to configure in **Avatar**, and the appropriate `.avatar_setup.json`
file in **Avatar Setup Json Asset**.  
Then click on `Add emotions`.

If everything goes fine, the animation controller, animation files, VRC Menu
parameters and files should be setup automatically.  
Else... you might want to open a bug report, and paste the content of the
'Console', but be reminded that I can only really help you if the model you're
configuring is openly accessible.

Limitations
---------------

Many.

The first one is that the emotions blendshapes 'short paths' are not
checked against the hierarchy. Your avatar is supposed to have all its meshes
as a direct descendant of the avatar root.  
This might be fixed in the future, if I can find models that actually don't follow
this convention.

The software has zero error reporting. Your best bet is to look for errors in the
Console. I'll fix this in the near future.

The VRChat submenus have generic names (Submenu0, Submenu1, ...).  
I'll add fields to setup prefixes in the near future. Meanwhile, you can edit the
generated menus, but be warned that you'll have to redo theses changed if
your reimport emotions on the same avatar.

Sometimes VRChat animations will bug, and the character will get stuck
with a specific emote... This don't last long, and opening the Debug menu
(Expressions menu -> Options -> Config -> Debug) generally solve the
situation, which kind of irks me...
