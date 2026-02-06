using System.Text;
using Holo.Data.Repositories.Catalogue;
using Holo.Data.Repositories.Furniture;

namespace Holo.Managers;

/// <summary>
/// Manager for catalogue page caching, catalogue item templates, catalogue purchase handling and few other catalogue related tasks.
/// </summary>
public static class catalogueManager
{
    private static Dictionary<string, cataloguePage> cataloguePages = new();
    private static Dictionary<int, itemTemplate> itemCache = new();

        /// <summary>
        /// Initializes the catalogue manager, (re)caching all the pages and item templates.
        /// </summary>
        public static void Init()
        {
            Out.WriteLine("Starting caching of catalogue + items...");

            int[] pageIDs = CatalogueRepository.Instance.GetPageIds();
            cataloguePages = new Dictionary<string, cataloguePage>();
            itemCache = new Dictionary<int, itemTemplate>();

            for (int i = 0; i < pageIDs.Length; i++)
            {
                cachePage(pageIDs[i]);
            }

            cachePage(-1);
            Out.WriteLine("Successfully cached " + cataloguePages.Count + " catalogue pages and " + itemCache.Count + " item templates!");
        }
        /// <summary>
        /// Caches a specified catalogue page, plus the items on this page.
        /// </summary>
        /// <param name="pageID">The ID of the page to cache. If -1 is specified, all the items that aren't on a page are cached.</param>
        private static void cachePage(int pageID)
        {
            string[] pageData = CatalogueRepository.Instance.GetPageData(pageID);
            if (pageID > 0 && pageData.Length == 0)
                return;

            string pageIndexName = "";
            StringBuilder pageBuilder = new System.Text.StringBuilder();
            cataloguePage objPage = new cataloguePage();

            if (pageID > 0)
            {
                pageIndexName = pageData[0];
                objPage.displayName = pageData[2];
                objPage.minRank = byte.Parse(pageData[1]);

                // Add the required fields for catalogue page (indexname, showname, page layout style (boxes etc)) 
                pageBuilder.Append("i:" + pageIndexName + Convert.ToChar(13) + "n:" + pageData[2] + Convert.ToChar(13) + "l:" + pageData[3] + Convert.ToChar(13));

                if (pageData[4] != "") // If there's a headline image set, add it 
                    pageBuilder.Append("g:" + pageData[4] + Convert.ToChar(13));
                if (pageData[5] != "")  // If there is/are side image(s) set, add it/them 
                    pageBuilder.Append("e:" + pageData[5] + Convert.ToChar(13));
                if (pageData[6] != "") // If there's a description set, add it 
                    pageBuilder.Append("h:" + pageData[6] + Convert.ToChar(13));
                if (pageData[8] != "") // If there's a 'Click here for more details' label set, add it 
                    pageBuilder.Append("w:" + pageData[8] + Convert.ToChar(13));
                if (pageData[7] != "") // If the misc additions field is not blank 
                {
                    string[] miscDetail = pageData[7].Split(Convert.ToChar(13));
                    for (int m = 0; m < miscDetail.Length; m++)
                        pageBuilder.Append(miscDetail[m] + Convert.ToChar(13));
                }
            }

            int[] itemTemplateIDs = CatalogueRepository.Instance.GetItemTemplateIds(pageID);
            int[] itemTypeIDs = CatalogueRepository.Instance.GetItemTypeIds(pageID);
            int[] itemLengths = CatalogueRepository.Instance.GetItemLengths(pageID);
            int[] itemWidths = CatalogueRepository.Instance.GetItemWidths(pageID);
            int[] itemCosts = CatalogueRepository.Instance.GetItemCosts(pageID);
            int[] itemDoorFlags = CatalogueRepository.Instance.GetItemDoorFlags(pageID);
            int[] itemTradeableFlags = CatalogueRepository.Instance.GetItemTradeableFlags(pageID);
            int[] itemRecycleableFlags = CatalogueRepository.Instance.GetItemRecycleableFlags(pageID);
            string[] itemNames = CatalogueRepository.Instance.GetItemNames(pageID);
            string[] itemDescs = CatalogueRepository.Instance.GetItemDescriptions(pageID);
            string[] itemCCTs = CatalogueRepository.Instance.GetItemCcts(pageID);
            string[] itemColours = CatalogueRepository.Instance.GetItemColours(pageID);
            string[] itemTopHs = CatalogueRepository.Instance.GetItemTopHeights(pageID);

            for (int i = 0; i < itemTemplateIDs.Length; i++)
            {
                if (stringManager.getStringPart(itemCCTs[i], 0, 4) != "deal")
                {
                    itemCache.Add(itemTemplateIDs[i], new itemTemplate(itemCCTs[i], Convert.ToByte(itemTypeIDs[i]), itemColours[i], itemLengths[i], itemWidths[i], double.Parse(itemTopHs[i]), (itemDoorFlags[i] == 1), (itemTradeableFlags[i] == 1), (itemRecycleableFlags[i] == 1)));
                    if (pageID == -1)
                        continue;

                    pageBuilder.Append("p:" + itemNames[i] + Convert.ToChar(9) + itemDescs[i] + Convert.ToChar(9) + itemCosts[i] + Convert.ToChar(9) + Convert.ToChar(9));

                    if (itemTypeIDs[i] == 0)
                        pageBuilder.Append("i");
                    else
                        pageBuilder.Append("s");

                    pageBuilder.Append(Convert.ToChar(9) + itemCCTs[i] + Convert.ToChar(9));

                    if (itemTypeIDs[i] == 0)
                        pageBuilder.Append(Convert.ToChar(9));
                    else
                        pageBuilder.Append("0" + Convert.ToChar(9));

                    if (itemTypeIDs[i] == 0)
                        pageBuilder.Append(Convert.ToChar(9));
                    else
                        pageBuilder.Append(itemLengths[i] + "," + itemWidths[i] + Convert.ToChar(9));

                    pageBuilder.Append(itemCCTs[i] + Convert.ToChar(9));

                    if (itemTypeIDs[i] > 0)
                        pageBuilder.Append(itemColours[i]);

                    pageBuilder.Append(Convert.ToChar(13));
                }
                else
                {
                    int dealID = int.Parse(itemCCTs[i].Substring(4));
                    int[] dealItemIDs = CatalogueRepository.Instance.GetDealItemIds(dealID);
                    int[] dealItemAmounts = CatalogueRepository.Instance.GetDealItemAmounts(dealID);

                    pageBuilder.Append("p:" + itemNames[i] + Convert.ToChar(9) + itemDescs[i] + Convert.ToChar(9) + itemCosts[i] + Convert.ToChar(9) + Convert.ToChar(9) + "d");
                    pageBuilder.Append(Convert.ToChar(9), 4);
                    pageBuilder.Append("deal" + dealID + Convert.ToChar(9) + Convert.ToChar(9) + dealItemIDs.Length + Convert.ToChar(9));

                    for (int y = 0; y < dealItemIDs.Length; y++)
                    {
                        string itemCCT = CatalogueRepository.Instance.GetItemCctByTemplateId(dealItemIDs[y]) ?? "";
                        string itemColour = CatalogueRepository.Instance.GetItemColourByTemplateId(dealItemIDs[y]) ?? "";
                        pageBuilder.Append(itemCCT + Convert.ToChar(9) + dealItemAmounts[y] + Convert.ToChar(9) + itemColour + Convert.ToChar(9));
                    }
                }
            }

            if (pageID == -1)
                return;

            objPage.pageData = pageBuilder.ToString();
            cataloguePages.Add(pageIndexName, objPage);
        }
        /// <summary>
        /// Returns a bool that specifies if the catalogue manager contains a certain page, specified by name.
        /// </summary>
        /// <param name="pageName">The name of the catalogue page to check.</param>
        public static bool getPageExists(string pageName)
        {
            return cataloguePages.ContainsKey(pageName);
        }
        /// <summary>
        /// Returns the index of catalogue pages for a certain user rank.
        /// </summary>
        /// <param name="userRank">The rank of the user to handout the index to.</param>
        public static string getPageIndex(byte userRank)
        {
            try
            {
                StringBuilder listBuilder = new StringBuilder();
                string[] pageNames = CatalogueRepository.Instance.GetPageNamesForRank(userRank);

                for (int i = 0; i < pageNames.Length; i++)
                {
                    if (cataloguePages.TryGetValue(pageNames[i], out var page))
                        listBuilder.Append(pageNames[i] + Convert.ToChar(9) + page.displayName + Convert.ToChar(13));
                }

                return listBuilder.ToString();
            }

            catch
            {
                return Convert.ToChar(13).ToString();
            }
        }
        /// <summary>
        /// Returns the content of a certain catalogue page as string.
        /// </summary>
        /// <param name="pageName">The name of the catalogue page to retrieve the content of.</param>
        /// <param name="userRank">The rank of the user to handout the page content to. If this rank is lower than the required minimum rank to access this page, the 'access denied' cast is returned.</param>
        /// <returns></returns>
        public static string getPage(string pageName, byte userRank)
        {
            if (cataloguePages.TryGetValue(pageName, out var objPage))
            {
                if (userRank < objPage.minRank)
                    return "holo.cast.catalogue.access_denied";

                return objPage.pageData;
            }

            return "cast_catalogue.access_denied";
        }
        /// <summary>
        /// Handles special actions at purchase in the catalogue, such as decoration variables for items and items who are sold in pairs.
        /// </summary>
        /// <param name="templateID">The template ID of the item being purchased.</param>
        /// <param name="receiverID">The ID of the user that receives the item in his/her Hand.</param>
        /// <param name="roomID">The target room ID of this item. 0 = inhand, -1 = in presentbox, -2 = in Recycler</param>
        /// <param name="decorID">The wallpaper/floor value for wallpaper/floor purchases.</param>
        /// <param name="presentBoxID">If the item is bought as present, then specify the ID of the present box item. If not, specify 0.</param>
        public static void handlePurchase(int templateID, int receiverID, int roomID, string decorID, int presentBoxID)
        {
            string Sprite = getTemplate(templateID).Sprite;
            bool handlePresentbox = true;
            switch (Sprite)
            {
                case "landscape":
                case "wallpaper":
                case "floor":
                    {
                        int itemID = lastItemID;
                        FurnitureRepository.Instance.UpdateVar(itemID, decorID);
                        break;
                    }

                case "roomdimmer":
                    {
                        int itemID = lastItemID;
                        string defaultPreset = "1,#000000,155";
                        string defaultSetPreset = "1,1,1,#000000,155";
                        FurnitureExtrasRepository.Instance.CreateMoodlight(itemID, 0, defaultPreset, defaultSetPreset);
                        FurnitureRepository.Instance.UpdateVar(itemID, defaultSetPreset);
                        break;
                    }

                case "door":
                case "doorB":
                case "doorC":
                case "doorD":
                case "teleport_door":
                case "xmas08_telep":
                case "ads_cltele":
                    {
                        int itemID1 = lastItemID;
                        int itemID2 = (int)FurnitureRepository.Instance.CreateItemWithTeleport(templateID, receiverID, roomID, itemID1);
                        FurnitureRepository.Instance.SetTeleporterId(itemID1, itemID2);
                        if (presentBoxID > 0)
                        {
                            FurnitureExtrasRepository.Instance.AddPresentItem(presentBoxID, itemID1);
                            FurnitureExtrasRepository.Instance.AddPresentItem(presentBoxID, itemID2);
                        }
                        handlePresentbox = false;
                        break;
                    }

                case "post.it":
                case "post.it.vd":
                    {
                        int itemID = lastItemID;
                        FurnitureRepository.Instance.UpdateVar(itemID, "20");
                        break;
                    }

                default:
                    {
                        if (stringManager.getStringPart(Sprite, 0, 10) == "sound_set_")
                        {
                            int itemID = lastItemID;
                            int soundSet = int.Parse(Sprite.Substring(10));
                            FurnitureRepository.Instance.SetSoundMachineSoundSet(itemID, soundSet);
                        }
                        break;
                    }
            }

            if (presentBoxID > 0 && handlePresentbox)
            {
                FurnitureExtrasRepository.Instance.AddPresentItem(presentBoxID, lastItemID);
            }
        }
        /// <summary>
        /// Returns the last purchased/created item in the 'furniture' table of the HoloDB.
        /// </summary>
        public static int lastItemID
        {
            get
            {
                return FurnitureRepository.Instance.GetLastInsertedId();
            }
        }
        public static string tradeItemList(int[] itemIDs)
        {
            StringBuilder List = new StringBuilder();
            for (int i = 0; i < itemIDs.Length; i++)
            {
                if (itemIDs[i] == 0)
                    continue;

                int templateID = FurnitureRepository.Instance.GetTemplateId(itemIDs[i]);
                itemTemplate Template = getTemplate(templateID);
                List.Append("SI" + Convert.ToChar(30).ToString() + itemIDs[i] + Convert.ToChar(30).ToString() + i + Convert.ToChar(30));
                if (Template.typeID > 0)
                    List.Append("S");
                else
                    List.Append("I");
                List.Append(Convert.ToChar(30).ToString() + itemIDs[i] + Convert.ToChar(30).ToString() + Template.Sprite + Convert.ToChar(30));
                if (Template.typeID > 0) { List.Append(Template.Length + Convert.ToChar(30).ToString() + Template.Width + Convert.ToChar(30).ToString() + (FurnitureRepository.Instance.GetVar(itemIDs[i]) ?? "") + Convert.ToChar(30).ToString()); }
                List.Append(Template.Colour + Convert.ToChar(30).ToString() + i + Convert.ToChar(30).ToString() + "/");
            }
            return List.ToString();
        }
        /// <summary>
        /// Checks if the wallposition for a wallitem is correct, if it is, then the output should be exactly the same as the input. If not, then the wallposition is invalid.
        /// </summary>
        /// <param name="wallPosition">The original wallposition. [input]</param>
        public static string wallPositionOK(string wallPosition)
        {
            //:w=3,2 l=9,63 l
            try
            {
                string[] posD = wallPosition.Split(' ');
                if (posD[2] != "l" && posD[2] != "r")
                    return "";

                string[] widD = posD[0].Substring(3).Split(',');
                int widthX = int.Parse(widD[0]);
                int widthY = int.Parse(widD[1]);
                if (widthX < 0 || widthY < 0 || widthX > 200 || widthY > 200)
                    return "";

                string[] lenD = posD[1].Substring(2).Split(',');
                int lengthX = int.Parse(lenD[0]);
                int lengthY = int.Parse(lenD[1]);
                if (lengthX < 0 || lengthY < 0 || lengthX > 200 || lengthY > 200)
                    return "";

                return ":w=" + widthX + "," + widthY + " " + "l=" + lengthX + "," + lengthY + " " + posD[2];
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Represents a page in the Catalogue.
        /// </summary>
        private struct cataloguePage
        {
            /// <summary>
            /// The display name of the page in the client.
            /// </summary>
            internal string displayName;
            /// <summary>
            /// The page string of the page, containing the layout, items etc.
            /// </summary>
            internal string pageData;
            /// <summary>
            /// The minimum rank that a virtual user requires to access this rank.
            /// </summary>
            internal byte minRank;
        }
        /// <summary>
        /// Represents a cached virtual item template.
        /// </summary>
        public struct itemTemplate
        {
            /// <summary>
            /// The type ID of the item, eg, 0 = walliten, 1 = flooritem, 2 = seat etc.
            /// </summary>
            internal byte typeID;
            /// <summary>
            /// The sprite of the item.
            /// </summary>
            internal string Sprite;
            /// <summary>
            /// The colour of the item.
            /// </summary>
            internal string Colour;
            /// <summary>
            /// The length of the item.
            /// </summary>
            internal int Length;
            /// <summary>
            /// The width of the item.
            /// </summary>
            internal int Width;
            /// <summary>
            /// The topheight of the item, if seat, then this indicates the sitheight. If a solid stackable item, then this is the stackheight. If 0.0, then the item is classified as non-stackable.
            /// </summary>
            internal double topH;
            /// <summary>
            /// Specifies if the item can be used as door.
            /// </summary>
            internal bool isDoor;
            /// <summary>
            /// Specifies if the item can be traded between virtual users.
            /// </summary>
            internal bool isTradeable;
            /// <summary>
            /// Specifies if the item can be recycled in the item recycler. 
            /// </summary>
            internal bool isRecycleable;
            public itemTemplate(bool b)
            {
                this.typeID = 1;
                this.Sprite = "";
                this.Colour = "null";
                this.Length = 1;
                this.Width = 1;
                this.topH = 0.0;
                this.isDoor = false;
                this.isTradeable = false;
                this.isRecycleable = false;
            }
            /// <summary>
            /// Initializes the item template.
            /// </summary>
            /// <param name="Sprite">The sprite of the item.</param>
            /// <param name="typeID">The type ID of the item, eg, 0 = walliten, 1 = flooritem, 2 = seat etc.</param>
            /// <param name="Colour">The colour of the item.</param>
            /// <param name="Length">The length of the item.</param>
            /// <param name="Width">The width of the item.</param>
            /// <param name="topH">The topheight of the item, if seat, then this indicates the sitheight. If a solid stackable item, then this is the stackheight. If 0.0, then the item is classified as non-stackable.</param>
            /// <param name="isDoor">Specifies if the item can be used as door.</param>
            /// <param name="isTradeable">Specifies if the item can be traded between virtual users.</param>
            /// <param name="isRecycleable">Specifies if the item can be recycled in the item recycler.</param>
            public itemTemplate(string Sprite, byte typeID, string Colour, int Length, int Width, double topH, bool isDoor, bool isTradeable, bool isRecycleable)
            {
                if (Sprite.Contains(" "))
                {
                    this.Sprite = Sprite.Split(' ')[0];
                    this.Colour = Sprite.Split(' ')[1];
                }
                else
                {
                    this.Sprite = Sprite;
                    this.Colour = Colour;
                }
                this.typeID = typeID;
                this.Length = Length;
                this.Width = Width;
                this.topH = topH;
                this.isDoor = isDoor;
                this.isTradeable = isTradeable;
                this.isRecycleable = isRecycleable;
            }
        }

        /// <summary>
        /// Returns the itemTemplate object matching a certain template ID. If the specified item template is not loaded, then an empty item template is returned.
        /// </summary>
        /// <param name="templateID">The template ID to return the item template of.</param>
        public static itemTemplate getTemplate(int templateID)
        {
            return itemCache.TryGetValue(templateID, out var template) ? template : new itemTemplate();
        }

        internal static void handlePurchase(int rewardTemplateID, int userID, int p, int p_4, int p_5)
        {
            throw new NotImplementedException();
        }
    }