# Problem
Non-approved workshop skins can be used in-game just like their officially sanctioned counterparts, but unlike those, "unofficial" skins are only downloaded and cached by the client the first time they're needed - and forgotten about after the player shuts down Rust. For example, it happens when a player opens a loot container with items skinned as non-approved workshop skins. Or since very recently, also when the player has the icons of those items displayed in custom UI elements on-screen.

Not having the skins cached and ready client-side BEFORE they're needed means it often takes anywhere from a few seconds up to a few minutes to have all the skins downloaded, especially if several custom skin IDs are mentioned to the client all at once. While a skin is being downloaded, both the item model (if applicable) and the item's icon present as their unskinned vanilla variants.

This is, at the very least, slightly inconvenient, and at most - mildly infuriating, as pictured below:

# Solution
Server admins can add any number of skin IDs to the configuration (although it's not recommended to have more than 30-50).  As soon as a player connects to the server, a dummy, invisible UI requesting all of those skin IDs will be immediately sent to the client. Not for displaying, but for preloading asynchronously in the background.

Next time during the session when the client needs those particular skin IDs for aesthetics/icons/UI elements, they'll be ready to be served without any delays.

Preloading happens for all players without the opt-out permission...
* when they connect
* when Skin Preloader plugin is loaded/reloaded
* when skins are added/removed with commands/hooks (see below).

# Permissions
> * **skinpreloader.admin**
> When you run any of the Admin Commands below as a player (from chat/F1 console), you need this permission.

> * **skinpreloader.opt_out**
> Grant this permission to players/groups that don't deserve to bask in the glory of instantly available custom skins.

# Configuration
The only data stored on the server is the list of skin IDs. You can find the data file under **/oxide/config/SkinPreloader.json**. By default this list is empty - use your favourite text editor or the commands/hooks below to populate it, like so:

    {
      "SkinList": [
        2144524645,
        2144547783,
        2146665840,
        2144560388,
        2144555007,
        2144558893,
        2567551241,
        2567552797,
        2756133263,
        2756136166
      ]
    }

# Admin Commands
These commands can be run from the server console, F1 console or the chat (in that case, precede the command with "/"). If not ran from the server console, the player executing them needs the **skinpreloader.admin** permission (see above).

> * **sp.list**
> Will list all currently registered skin IDs in the chat or console. If no skins are currently registered, it will also let the admin executing know.

> * **sp.add** 111 222 333 444 555 ...
> Will add a single skin or a range of skins to the config (separate multiple numbers with a space) and inform the admin executing whether adding them was successful (true) or (not).

> * **sp.remove** 111 222 333 444 555 ...
> Will remove a single skin or a range of skins from the config (separate multiple numbers with a space) and inform the admin executing whether removing them was successful (true) or (not).

> * **sp.remove.all**
> Will remove every single skin from your config, if there's any registered, and dinform the admin executing how many were removed. WARNING: There's no undo.

# For Developers
Add the following code to the top level of your plugin class:

    [PluginReference]
    private Plugin SkinPreloader;

During server boot, plugins are loaded in alphabetical order; chances are your plugin might load before Skin Preloader. **Make sure you wait at least 1 frame after your OnServerInitialized() method before calling any of the hooks below.**

    //returns an array containing all skins registered. If nothing registered, returns null
    ulong[] skinList = (ulong[])SkinPreloader?.CallHook("SkinList");
    
    //returns true if skin 2345678901 is registered in the config right now
    bool isSkinRegistered = (bool)SkinPreloader?.CallHook("SkinExists", 2345678901U);
    
    //tries to add a single skin 2345678901 to the config and returns true if the skin was NOT registered before, otherwise false
    bool tryAddSkinResult = (bool)SkinPreloader?.CallHook("SkinAddSingle", 2345678901U);
     
    //tries to remove a single skin 2345678901 from the config and returns true if the skin WAS registered before, otherwise false
    bool tryRemoveSkinResult = (bool)SkinPreloader?.CallHook("SkinRemoveSingle", 2345678901U);
    
    //tries to add any IEnumerable<ulong> of skin IDs to the config. Returns an array of true/false values (whether a particular skin ID from the IEnumerable was just added). 
    bool[] addSkinRangeResults = (bool[])SkinPreloader?.CallHook("SkinAddRange", new List<ulong>{1, 2, 3, 4, 5});
    
    //tries to remove any IEnumerable<ulong> of skin IDs from the config. Returns an array of true/false values (whether a particular skin ID from the IEnumerable was just removed). 
    bool[] removeSkinRangeResults = (bool[])SkinPreloader?.CallHook("SkinRemoveRange", new List<ulong>{1, 2, 3, 4, 5});
    
    //Remove all skins from the config and returns the number of entries that were just removed
    int numSkinsRemoved = (int)SkinPreloader?.CallHook("SkinRemoveAll"); 