using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Oxide.Plugins
{
    [Info("Skin Preloader", "Nikedemos", "1.0.0")]
    [Description("Forces connected players to download and cache custom Steam Workshop skin IDs immediately after spawning so they can be used for icons as soon as possible")]
    public class SkinPreloader : RustPlugin
    {
        #region CONST/STATIC
        public static SkinPreloader Instance;

        public const string CMD_ADD = "sp.add";
        public const string CMD_REMOVE = "sp.remove";
        public const string CMD_LIST = "sp.list";
        public const string CMD_REMOVE_ALL = "sp.remove.all";

        public const string ITEM_RUG_SHORTNAME = "rug";
        public static int ItemRugID = 0;

        #endregion

        #region PERMISSIONS

        public const string PERM_SUFFIX_ADMIN = "admin";
        public const string PERM_SUFFIX_OPT_OUT = "opt_out";

        public static string PERMISSION_OPT_OUT;
        public static string PERMISSION_ADMIN;
        #endregion

        #region HOOKS
        void OnServerInitialized()
        {
            Instance = this;

            lang.RegisterMessages(LangMessages, this);

            ItemRugID = ItemManager.FindItemDefinition(ITEM_RUG_SHORTNAME).itemid;

            LoadConfigData();


            PERMISSION_OPT_OUT = $"{nameof(SkinPreloader).ToLower()}.{PERM_SUFFIX_OPT_OUT}";
            PERMISSION_ADMIN = $"{nameof(SkinPreloader).ToLower()}.{PERM_SUFFIX_ADMIN}";

            permission.RegisterPermission(PERMISSION_OPT_OUT, this);
            permission.RegisterPermission(PERMISSION_ADMIN, this);

            AddCovalenceCommand(CMD_LIST, nameof(CommandList), PERMISSION_ADMIN);
            AddCovalenceCommand(CMD_ADD, nameof(CommandAdd), PERMISSION_ADMIN);
            AddCovalenceCommand(CMD_REMOVE, nameof(CommandRemove), PERMISSION_ADMIN);
            AddCovalenceCommand(CMD_REMOVE_ALL, nameof(CommandClear), PERMISSION_ADMIN);

            DummyGuiManager.OnServerInitialized();

            //add defaults if missing
            ProcessConfigData();
        }

        void Unload()
        {
            DummyGuiManager.Unload();

            Instance = null;
            PERMISSION_OPT_OUT = null;
            ItemRugID = 0;
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            if (Instance == null)
            {
                return;
            }

            DummyGuiManager.Show(player);
        }
        #endregion

        #region LANG

        public const string MSG_PLEASE_PROVIDE_AT_LEAST_1_VALID = nameof(MSG_PLEASE_PROVIDE_AT_LEAST_1_VALID);
        public const string MSG_OK_ADDED_SKIN_ID = nameof(MSG_OK_ADDED_SKIN_ID);
        public const string MSG_OK_REMOVED_SKIN_ID = nameof(MSG_OK_REMOVED_SKIN_ID);
        public const string MSG_SKIN_ALREADY_EXISTS = nameof(MSG_SKIN_ALREADY_EXISTS);
        public const string MSG_ERROR_SKIN_DOESNT_EXIST = nameof(MSG_ERROR_SKIN_DOESNT_EXIST);

        public const string MSG_SKIN_INVALID = nameof(MSG_SKIN_INVALID);

        public const string MSG_REGENERATING_CACHE_LIST = nameof(MSG_REGENERATING_CACHE_LIST);

        public const string MSG_NO_SKIN_IDS_EXIST = nameof(MSG_NO_SKIN_IDS_EXIST);
        public const string MSG_FOLLOWING_SKIN_IDS_EXIST = nameof(MSG_FOLLOWING_SKIN_IDS_EXIST);

        public const string MSG_REMOVED_N_SKIN_IDS = nameof(MSG_REMOVED_N_SKIN_IDS);

        public readonly Dictionary<string, string> LangMessages = new Dictionary<string, string>
        {
            [MSG_PLEASE_PROVIDE_AT_LEAST_1_VALID] = "Please provide at least 1 valid skin ID",
            [MSG_OK_ADDED_SKIN_ID] = "OK: Added skin ID {0}",
            [MSG_OK_REMOVED_SKIN_ID] = "OK: Removed skin ID {0}",

            [MSG_SKIN_ALREADY_EXISTS] = "ERROR: skin ID {0} already exists",
            [MSG_ERROR_SKIN_DOESNT_EXIST] = "ERROR: skin ID {0} doesn't exist",

            [MSG_SKIN_INVALID] = "ERROR: {0} is not a valid skin ID",

            [MSG_REGENERATING_CACHE_LIST] = "Regenerating cache list...",

            [MSG_NO_SKIN_IDS_EXIST] = "There's no skin IDs registered",
            [MSG_FOLLOWING_SKIN_IDS_EXIST] = "The following skin IDs are registered:",
            [MSG_REMOVED_N_SKIN_IDS] = "Removed {0} skin IDs",
        };

        private static string MSG(string msg, string userID = null, params object[] args)
        {
            if (args == null)
            {
                return Instance.lang.GetMessage(msg, Instance, userID);
            }
            else
            {
                return string.Format(Instance.lang.GetMessage(msg, Instance, userID), args);
            }

        }
        #endregion

        #region COMMAND
        [Command(CMD_LIST)]
        private void CommandList(IPlayer iplayer, string command, string[] args)
        {
            //if (!HasAdminPermission(iplayer)) return;
            StringBuilder builder = new StringBuilder();

            if (Configuration.SkinList.Count == 0)
            {
                builder.Append(MSG(MSG_NO_SKIN_IDS_EXIST, iplayer.Id));
            }
            else
            {
                builder.AppendLine(MSG(MSG_FOLLOWING_SKIN_IDS_EXIST, iplayer.Id));

                var skinList = SkinList();
                for (var i = 0; i < skinList.Length; i++)
                {
                    builder.Append(skinList[i]);

                    if (i < skinList.Length-1)
                    {
                        builder.Append(' ');
                    }
                }
            }

            iplayer.Reply(builder.ToString());
        }

        [Command(CMD_ADD)]
        private void CommandAdd(IPlayer iplayer, string command, string[] args)
        {
            if (args.Length == 0)
            {
                iplayer.Reply(MSG(MSG_PLEASE_PROVIDE_AT_LEAST_1_VALID, iplayer.Id));
                return;
            }

            if (args.Length == 1)
            {
                //add single
                ulong skinID;

                if (ulong.TryParse(args[0], out skinID))
                {
                    if (SkinAddSingle(skinID))
                    {
                        iplayer.Reply(MSG(MSG_OK_ADDED_SKIN_ID, iplayer.Id, args[0]));
                    }
                    else
                    {
                        iplayer.Reply(MSG(MSG_SKIN_ALREADY_EXISTS, iplayer.Id, args[0]));
                    }
                }
                else
                {
                    iplayer.Reply(MSG(MSG_SKIN_INVALID, iplayer.Id, args[0]));
                }
            }
            else
            {
                var skinList = Facepunch.Pool.GetList<ulong>();

                for (var i = 0; i < args.Length; i++)
                {
                    ulong skinID;

                    if (ulong.TryParse(args[i], out skinID))
                    {
                        skinList.Add(skinID);
                    }
                }

                if (skinList.Count == 0)
                {
                    iplayer.Reply(MSG(MSG_PLEASE_PROVIDE_AT_LEAST_1_VALID, iplayer.Id));
                    return;
                }

                var addingResult = SkinAddRange(skinList);

                StringBuilder builder = new StringBuilder();

                for (var i = 0; i< addingResult.Length; i++)
                {
                    if (addingResult[i])
                    {
                        iplayer.Reply(MSG(MSG_OK_ADDED_SKIN_ID, iplayer.Id, skinList[i]));
                    }
                    else
                    {
                        iplayer.Reply(MSG(MSG_SKIN_ALREADY_EXISTS, iplayer.Id, skinList[i]));
                    }
                }

                Facepunch.Pool.FreeList(ref skinList);

                iplayer.Reply(builder.ToString());
            }
        }

        [Command(CMD_REMOVE)]
        private void CommandRemove(IPlayer iplayer, string command, string[] args)
        {
            if (args.Length == 0)
            {
                iplayer.Reply(MSG(MSG_PLEASE_PROVIDE_AT_LEAST_1_VALID, iplayer.Id));
                return;
            }

            if (args.Length == 1)
            {
                //add single
                ulong skinID;

                if (ulong.TryParse(args[0], out skinID))
                {
                    if (SkinRemoveSingle(skinID))
                    {
                        iplayer.Reply(MSG(MSG_OK_REMOVED_SKIN_ID, iplayer.Id, args[0]));
                    }
                    else
                    {
                        iplayer.Reply(MSG(MSG_ERROR_SKIN_DOESNT_EXIST, iplayer.Id, args[0]));
                    }
                }
                else
                {
                    iplayer.Reply(MSG(MSG_SKIN_INVALID, iplayer.Id, args[0]));
                }
            }
            else
            {
                var skinList = Facepunch.Pool.GetList<ulong>();

                for (var i = 0; i < args.Length; i++)
                {
                    ulong skinID;

                    if (ulong.TryParse(args[i], out skinID))
                    {
                        skinList.Add(skinID);
                    }
                }

                if (skinList.Count == 0)
                {
                    iplayer.Reply(MSG(MSG_PLEASE_PROVIDE_AT_LEAST_1_VALID, iplayer.Id));
                    return;
                }

                var removingResult = SkinRemoveRange(skinList);

                StringBuilder builder = new StringBuilder();

                for (var i = 0; i < removingResult.Length; i++)
                {
                    if (removingResult[i])
                    {
                        iplayer.Reply(MSG(MSG_OK_REMOVED_SKIN_ID, iplayer.Id, skinList[i]));
                    }
                    else
                    {
                        iplayer.Reply(MSG(MSG_ERROR_SKIN_DOESNT_EXIST, iplayer.Id, skinList[i]));
                    }
                }

                Facepunch.Pool.FreeList(ref skinList);

                iplayer.Reply(builder.ToString());
            }
        }

        [Command(CMD_REMOVE_ALL)]
        private void CommandClear(IPlayer iplayer, string command, string[] args)
        {
            int numRemoved = SkinRemoveAll();

            if (numRemoved == 0)
            {
                iplayer.Reply(MSG(MSG_NO_SKIN_IDS_EXIST, iplayer.Id));
                return;
            }

            iplayer.Reply(MSG(MSG_REMOVED_N_SKIN_IDS, iplayer.Id, numRemoved));
        }
        #endregion

        #region API
        [HookMethod(nameof(SkinExists))]
        public bool SkinExists(ulong skinID)
        {
            return Configuration.SkinList.Contains(skinID);
        }

        [HookMethod(nameof(SkinAddSingle))]
        public bool SkinAddSingle(ulong skinID)
        {
            if (Configuration.SkinList.Contains(skinID))
            {
                return false;
            }

            Configuration.SkinList.Add(skinID);
            DummyGuiManager.Regenerate();
            return true;
        }

        [HookMethod(nameof(SkinAddRange))]
        public bool[] SkinAddRange(IEnumerable<ulong> skinIDs)
        {
            if (skinIDs == null)
            {
                return null;
            }

            if (!skinIDs.Any())
            {
                return null;
            }

            List<bool> resultsList = Facepunch.Pool.GetList<bool>();

            bool needsUpdate = false;

            foreach (var skinID in skinIDs)
            {
                if (Configuration.SkinList.Contains(skinID))
                {
                    resultsList.Add(false);
                }
                else
                {
                    needsUpdate = true;
                    Configuration.SkinList.Add(skinID);

                    resultsList.Add(true);
                }
            }

            if (needsUpdate)
            {
                DummyGuiManager.Regenerate();
            }

            bool[] results = resultsList.ToArray();

            Facepunch.Pool.FreeList(ref resultsList);

            return results;
        }

        [HookMethod(nameof(SkinRemoveSingle))]
        public bool SkinRemoveSingle(ulong skinID)
        {
            if (!Configuration.SkinList.Contains(skinID))
            {
                return false;
            }

            Configuration.SkinList.Remove(skinID);
            DummyGuiManager.Regenerate();
            return true;
        }

        [HookMethod(nameof(SkinRemoveRange))]
        public bool[] SkinRemoveRange(IEnumerable<ulong> skinIDs)
        {
            if (skinIDs == null)
            {
                return null;
            }

            if (!skinIDs.Any())
            {
                return null;
            }

            List<bool> resultsList = Facepunch.Pool.GetList<bool>();

            bool needsUpdate = false;

            foreach (var skinID in skinIDs)
            {
                if (!Configuration.SkinList.Contains(skinID))
                {
                    resultsList.Add(false);
                }
                else
                {
                    needsUpdate = true;
                    Configuration.SkinList.Remove(skinID);

                    resultsList.Add(true);
                }
            }

            if (needsUpdate)
            {
                DummyGuiManager.Regenerate();
            }

            bool[] results = resultsList.ToArray();

            Facepunch.Pool.FreeList(ref resultsList);

            return results;
        }

        [HookMethod(nameof(SkinRemoveAll))]
        public int SkinRemoveAll()
        {
            if (Configuration.SkinList.Count == 0)
            {
                return 0;
            }

            int numRemoved = Configuration.SkinList.Count;

            Configuration.SkinList.Clear();
            DummyGuiManager.Regenerate();

            return numRemoved;
        }

        [HookMethod(nameof(SkinList))]
        public ulong[] SkinList()
        {
            if (Configuration.SkinList.Count == 0)
            {
                return null;
            }

            return Configuration.SkinList.ToArray();
        }

        #endregion

        #region CONFIG

        public ConfigData Configuration;

        public class ConfigData
        {
            public List<ulong> SkinList = new List<ulong>();
            public bool PopulateEmptyListWithDefaultsOnReload = true;
        }

        protected override void LoadDefaultConfig()
        {
            RestoreDefaultConfig();
        }

        private void ProcessConfigData()
        {
            if (Configuration.PopulateEmptyListWithDefaultsOnReload)
            {
                if (Configuration.SkinList.Count == 0)
                {
                    SkinAddRange(new List<ulong>
                    {
                        //Vehicle Airdrops
                        2144524645,
                        2144547783,
                        2146665840,
                        2144560388,
                        2144555007,
                        2144558893,
                        2567551241,
                        2567552797,
                        2756133263,
                        2756136166,

                        //Water Bases
                        2484982352,
                        2485021365,

                        //Grappling Hook
                        2387182643,
                    });
                }
            }


        }

        private void LoadConfigData()
        {
            try
            {
                Configuration = Config.ReadObject<ConfigData>();
            }
            catch
            {
                RestoreDefaultConfig();
            }

        }
        private void SaveConfigData()
        {
            Config.WriteObject(Configuration, true);
        }

        private void RestoreDefaultConfig()
        {
            Configuration = new ConfigData();

            SaveConfigData();
        }

        #endregion

        #region DUMMY GUI
        public static class DummyGuiManager
        {
            public const string ELEMENT_PARENT_NAME = "sp.parent";

            public static CuiRectTransformComponent DummyTransform;

            public static CuiElementContainer DummyContainer;
            public static string DummyContainerJSON;

            public static CuiElement DummyParent;

            public static void OnServerInitialized()
            {
                DummyTransform = new CuiRectTransformComponent
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                };

                DummyParent = new CuiElement
                {
                    Name = ELEMENT_PARENT_NAME,
                    Parent = "Overlay",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = "-10001 -10001",
                            OffsetMax = "-10000 -10000",
                        }
                    }
                };

                DummyContainer = new CuiElementContainer();

                Regenerate();

                foreach (var player in BasePlayer.activePlayerList)
                {
                    Show(player);
                }
            }

            public static void Unload()
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    Hide(player);
                }

                DummyContainer = null;
                DummyContainerJSON = null;

                DummyParent = null;
                DummyTransform = null;
            }

            public static void Regenerate()
            {
                Instance.PrintWarning(MSG(MSG_REGENERATING_CACHE_LIST));

                DummyContainer.Clear();

                DummyContainer.Add(DummyParent);

                for (var i = 0; i< Instance.Configuration.SkinList.Count; i++)
                {
                    ulong skinID = Instance.Configuration.SkinList[i];

                    CuiElement currentElement = new CuiElement
                    {
                        Parent = ELEMENT_PARENT_NAME,
                        Components =
                        {
                            new CuiImageComponent
                            {
                                SkinId = skinID,
                                ItemId = ItemRugID,
                            },
                            DummyTransform
                        }
                    };

                    DummyContainer.Add(currentElement);

                }

                //re-bake it
                DummyContainerJSON = DummyContainer.ToJson();

                Instance.SaveConfigData();
            }

            public static void Show(BasePlayer player)
            {
                Hide(player);

                if (Instance.permission.UserHasPermission(player.UserIDString, PERMISSION_OPT_OUT))
                {
                    return;
                }

                CuiHelper.AddUi(player, DummyContainerJSON);
            }

            public static void Hide(BasePlayer player)
            {
                CuiHelper.DestroyUi(player, ELEMENT_PARENT_NAME);
            }
        }
        #endregion
    }
}
