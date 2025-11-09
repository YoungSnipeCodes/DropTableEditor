using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DropTableEditor.Engine;

namespace DropTableEditor.Windows
{
    /// <summary>
    /// Drop Table Viewer window.
    /// </summary>
    public partial class DropTableViewer : Window
    {
        private bool hasChanges = false;
        private int? scrollToDropNumber = null;

        /// <summary>
        /// Helper class to store item data for sorting.
        /// </summary>
        private class ItemData
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public int Level { get; set; }
        }

        /// <summary>
        /// Item category mapping.
        /// </summary>
        private static readonly Dictionary<int, string> ItemCategories = new Dictionary<int, string>()
        {
            { 1, "LIST_FACEITEM" },
            { 2, "LIST_CAP" },
            { 3, "LIST_BODY" },
            { 4, "LIST_ARMS" },
            { 5, "LIST_FOOT" },
            { 6, "LIST_BACK" },
            { 7, "LIST_JEWEL" },
            { 8, "LIST_WEAPON" },
            { 9, "LIST_SUBWPN" },
            { 10, "LIST_USEITEM" },
            { 11, "LIST_JEMITEM" },
            { 12, "LIST_NATURAL" },
            { 13, "LIST_NATURAL" },
            { 14, "LIST_PAT" }
        };

        /// <summary>
        /// Item category STL mapping.
        /// </summary>
        private static readonly Dictionary<int, string> ItemCategorySTLs = new Dictionary<int, string>()
        {
            { 1, "LIST_FACEITEM_S" },
            { 2, "LIST_CAP_S" },
            { 3, "LIST_BODY_S" },
            { 4, "LIST_ARMS_S" },
            { 5, "LIST_FOOT_S" },
            { 6, "LIST_BACK_S" },
            { 7, "LIST_JEWEL_S" },
            { 8, "LIST_WEAPON_S" },
            { 9, "LIST_SUBWPN_S" },
            { 10, "LIST_USEITEM_S" },
            { 11, "LIST_JEMITEM_S" },
            { 12, "LIST_NATURAL_S" },
            { 13, "LIST_NATURAL_S" },
            { 14, "LIST_PAT_S" }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="DropTableViewer"/> class.
        /// </summary>
        /// <param name="scrollToDropNumber">Optional drop number to scroll to and expand.</param>
        public DropTableViewer(int? scrollToDropNumber = null)
        {
            InitializeComponent();
            this.scrollToDropNumber = scrollToDropNumber;
            
            LoadDropTables();
            
            // Set focus to search box after window is loaded
            this.Loaded += (s, e) =>
            {
                SearchBox.Focus();
                System.Windows.Input.Keyboard.Focus(SearchBox);
            };
        }

        /// <summary>
        /// Gets the item name from item ID.
        /// </summary>
        private string GetItemName(int itemID)
        {
            if (itemID <= 0)
                return null;

            int category = itemID / 1000;
            int index = itemID % 1000;

            if (!ItemCategories.ContainsKey(category))
                return null;

            string stbKey = ItemCategories[category];
            string stlKey = ItemCategorySTLs[category];

            if (!FileManager.STBs.ContainsKey(stbKey))
                return null;

            var stb = FileManager.STBs[stbKey];

            if (index >= stb.Cells.Count || index < 0)
                return null;

            if (!FileManager.STLs.ContainsKey(stlKey))
                return null;

            // Determine which column to use for name ID
            int columnUsed;
            if (category >= 1 && category <= 6)
                columnUsed = 0;
            else if (category == 7)
                columnUsed = 1;
            else if (category >= 8 && category <= 9)
                columnUsed = 0;
            else if (category == 14)
                columnUsed = 0;
            else
                columnUsed = 1;

            string nameID = null;
            if (stb.Cells[index].Count > columnUsed)
                nameID = stb.Cells[index][columnUsed];

            if (string.IsNullOrEmpty(nameID) || nameID == "0")
                return null;

            string itemName = FileManager.STLs[stlKey].Search(nameID);
            return string.IsNullOrEmpty(itemName) ? null : itemName;
        }

        /// <summary>
        /// Gets the item level from item ID (for equipment).
        /// Level is stored in column 21, but only when column 20 (Stat Requirement 1) is 31 (Level).
        /// </summary>
        private int GetItemLevel(int itemID)
        {
            if (itemID <= 0)
                return 0;

            int category = itemID / 1000;
            int index = itemID % 1000;

            // Only equipment categories have level
            if (category < 1 || category > 9)
                return 0;

            if (!ItemCategories.ContainsKey(category))
                return 0;

            string stbKey = ItemCategories[category];

            if (!FileManager.STBs.ContainsKey(stbKey))
                return 0;

            var stb = FileManager.STBs[stbKey];

            if (index >= stb.Cells.Count || index < 0)
                return 0;

            // Check if column 20 (Stat Requirement 1) is 31 (Level)
            if (stb.Cells[index].Count > 20 && stb.Cells[index].Count > 21)
            {
                int statID;
                if (int.TryParse(stb.Cells[index][20], out statID) && statID == 31)
                {
                    // Column 21 is the level requirement
                    int level;
                    if (int.TryParse(stb.Cells[index][21], out level))
                        return level;
                }
            }

            return 0;
        }

        /// <summary>
        /// Gets the NPC names that use a specific drop table.
        /// </summary>
        private string GetNPCsUsingDropTable(int dropTableNumber)
        {
            var npcNames = new List<string>();
            
            if (!FileManager.STBs.ContainsKey("LIST_NPC") || !FileManager.STLs.ContainsKey("LIST_NPC_S"))
                return "";
            
            var npcStb = FileManager.STBs["LIST_NPC"];
            var npcStl = FileManager.STLs["LIST_NPC_S"];
            
            for (int i = 1; i < npcStb.Cells.Count; i++)
            {
                var row = npcStb.Cells[i];
                
                // Column 86 contains the drop table number for NPCs
                if (row.Count > 86)
                {
                    int npcDropTable;
                    if (int.TryParse(row[86], out npcDropTable) && npcDropTable == dropTableNumber)
                    {
                        // Get NPC name from column 1
                        if (row.Count > 1 && !string.IsNullOrEmpty(row[1]))
                        {
                            string npcName = npcStl.Search(row[1]);
                            if (!string.IsNullOrEmpty(npcName))
                                npcNames.Add(npcName);
                        }
                    }
                }
            }
            
            if (npcNames.Count == 0)
                return "";
            
            return string.Join(", ", npcNames.ToArray());
        }

        /// <summary>
        /// Handles the search button click.
        /// </summary>
        private void Search_Click(object sender, RoutedEventArgs e)
        {
            string searchText = SearchBox.Text.ToLower().Trim();
            
            if (string.IsNullOrEmpty(searchText))
            {
                MessageBox.Show("Please enter a search term.", "Search", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // Collapse all expanders first
            foreach (var child in DropTablesPanel.Children)
            {
                if (child is Expander)
                    ((Expander)child).IsExpanded = false;
            }
            
            bool foundAny = false;
            
            // Search through all drop tables
            for (int dropNumber = 1; dropNumber < FileManager.STBs["ITEM_DROP"].Cells.Count; dropNumber++)
            {
                var dropRow = FileManager.STBs["ITEM_DROP"].Cells[dropNumber];
                bool foundInTable = false;
                
                // Check if drop number matches search
                if (dropNumber.ToString().Contains(searchText))
                {
                    foundInTable = true;
                }
                
                // Check each item in the drop table
                for (int col = 0; col < dropRow.Count && !foundInTable; col++)
                {
                    int itemID = 0;
                    if (int.TryParse(dropRow[col], out itemID) && itemID > 0)
                    {
                        string itemName = GetItemName(itemID);
                        if (!string.IsNullOrEmpty(itemName) && itemName.ToLower().Contains(searchText))
                        {
                            foundInTable = true;
                            break;
                        }
                        
                        // Also check item ID
                        if (itemID.ToString().Contains(searchText))
                        {
                            foundInTable = true;
                            break;
                        }
                    }
                }
                
                // Check NPC names
                if (!foundInTable)
                {
                    string npcNames = GetNPCsUsingDropTable(dropNumber);
                    if (!string.IsNullOrEmpty(npcNames) && npcNames.ToLower().Contains(searchText))
                    {
                        foundInTable = true;
                    }
                }
                
                // Expand this table if found
                if (foundInTable)
                {
                    foundAny = true;
                    foreach (var child in DropTablesPanel.Children)
                    {
                        if (child is Expander)
                        {
                            var expander = (Expander)child;
                            if ((int)expander.Tag == dropNumber)
                            {
                                expander.IsExpanded = true;
                                expander.BringIntoView();
                                break;
                            }
                        }
                    }
                }
            }
            
            if (!foundAny)
            {
                MessageBox.Show("No drop tables found matching your search.", "Search", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Loads all drop tables.
        /// </summary>
        private void LoadDropTables()
        {
            DropTablesPanel.Children.Clear();

            if (!FileManager.STBs.ContainsKey("ITEM_DROP"))
            {
                DropTablesPanel.Children.Add(new TextBlock()
                {
                    Text = "ITEM_DROP.STB not loaded.",
                    Margin = new Thickness(5),
                    Foreground = Brushes.Black
                });
                return;
            }

            for (int dropNumber = 1; dropNumber < FileManager.STBs["ITEM_DROP"].Cells.Count; dropNumber++)
            {
                CreateDropTableExpander(dropNumber);
            }

            if (DropTablesPanel.Children.Count == 0)
            {
                DropTablesPanel.Children.Add(new TextBlock()
                {
                    Text = "No drop tables found.",
                    Margin = new Thickness(5),
                    Foreground = Brushes.Black
                });
            }
        }

        /// <summary>
        /// Creates an expander for a drop table.
        /// </summary>
        private void CreateDropTableExpander(int dropNumber)
        {
            var dropRow = FileManager.STBs["ITEM_DROP"].Cells[dropNumber];
            
            // Create the main panel with buttons
            var mainPanel = new DockPanel() { LastChildFill = true };

            // Create button panel
            var buttonPanel = new StackPanel() 
            { 
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(5)
            };

            var removeTableButton = new Button()
            {
                Content = "Remove Table",
                Width = 90,
                Height = 22,
                Margin = new Thickness(0, 0, 5, 0)
            };
            removeTableButton.Click += (s, e) => RemoveDropTable_Click(dropNumber);

            buttonPanel.Children.Add(removeTableButton);

            DockPanel.SetDock(buttonPanel, Dock.Top);
            mainPanel.Children.Add(buttonPanel);

            // Create the items panel - show ALL columns
            var itemsPanel = new StackPanel() { Margin = new Thickness(10, 5, 5, 5) };
            int totalColumns = dropRow.Count;
            int filledColumns = 0;

            for (int col = 0; col < totalColumns; col++)
            {
                string cellValue = dropRow[col];
                int itemID = 0;
                
                if (!string.IsNullOrEmpty(cellValue) && cellValue != "0")
                {
                    int.TryParse(cellValue, out itemID);
                    if (itemID > 0)
                        filledColumns++;
                }

                itemsPanel.Children.Add(CreateItemRow(dropNumber, col, itemID));
            }

            mainPanel.Children.Add(itemsPanel);

            // Get NPCs using this drop table
            string npcNames = GetNPCsUsingDropTable(dropNumber);
            string headerText = string.Format("Drop Table {0} ({1} Items)", dropNumber, filledColumns);
            if (!string.IsNullOrEmpty(npcNames))
            {
                headerText += string.Format(" - NPCs: {0}", npcNames);
            }

            // Create the expander
            var expander = new Expander()
            {
                Header = headerText,
                IsExpanded = scrollToDropNumber.HasValue && scrollToDropNumber.Value == dropNumber,
                Margin = new Thickness(0, 2, 0, 2),
                Content = mainPanel,
                Tag = dropNumber
            };

            DropTablesPanel.Children.Add(expander);

            // Scroll to this expander if it's the target
            if (scrollToDropNumber.HasValue && scrollToDropNumber.Value == dropNumber)
            {
                expander.Loaded += (s, e) =>
                {
                    expander.BringIntoView();
                };
            }
        }

        /// <summary>
        /// Creates a row for an item with edit/remove buttons.
        /// </summary>
        private DockPanel CreateItemRow(int dropNumber, int columnIndex, int itemID)
        {
            var rowPanel = new DockPanel() 
            { 
                Margin = new Thickness(0, 2, 0, 2),
                Background = Brushes.Transparent
            };

            // Make the row clickable for highlighting
            rowPanel.MouseLeftButtonDown += (s, e) =>
            {
                // Clear previous highlights in this drop table
                var parent = (s as DockPanel).Parent as StackPanel;
                if (parent != null)
                {
                    foreach (var child in parent.Children)
                    {
                        if (child is DockPanel)
                            ((DockPanel)child).Background = Brushes.Transparent;
                    }
                }

                // Highlight this row
                rowPanel.Background = new SolidColorBrush(Color.FromRgb(173, 216, 230)); // Light blue
            };

            rowPanel.MouseEnter += (s, e) =>
            {
                if (rowPanel.Background == Brushes.Transparent)
                    rowPanel.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)); // Light gray hover
            };

            rowPanel.MouseLeave += (s, e) =>
            {
                if (rowPanel.Background.ToString() == "#FFF0F0F0") // Only clear hover color
                    rowPanel.Background = Brushes.Transparent;
            };

            // Create button panel
            var buttonPanel = new StackPanel() 
            { 
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 5, 0)
            };

            var editButton = new Button()
            {
                Content = "Edit",
                Width = 50,
                Height = 20,
                Margin = new Thickness(0, 0, 2, 0),
                FontSize = 10
            };
            editButton.Click += (s, e) => EditItem_Click(dropNumber, columnIndex, itemID);

            buttonPanel.Children.Add(editButton);

            DockPanel.SetDock(buttonPanel, Dock.Right);
            rowPanel.Children.Add(buttonPanel);

            // Create item text
            var itemText = new TextBlock()
            {
                Foreground = Brushes.Black
            };

            // Show column number
            itemText.Inlines.Add(new System.Windows.Documents.Run(string.Format("Col {0}: ", columnIndex))
            {
                FontWeight = FontWeights.Bold
            });

            if (itemID > 0)
            {
                string itemName = GetItemName(itemID);
                
                // Get requirements and job to display on same line as name
                int category = itemID / 1000;
                int index = itemID % 1000;
                string levelAndReqs = "";
                
                if (category >= 1 && category <= 9 && ItemCategories.ContainsKey(category))
                {
                    string stbKey = ItemCategories[category];
                    if (FileManager.STBs.ContainsKey(stbKey))
                    {
                        var stb = FileManager.STBs[stbKey];
                        if (index < stb.Cells.Count && index >= 0)
                        {
                            var row = stb.Cells[index];
                            
                            // Job restrictions (cols 17-19)
                            var jobs = new List<string>();
                            if (row.Count > 17 && !string.IsNullOrEmpty(row[17]) && row[17] != "0")
                            {
                                int classCode;
                                if (int.TryParse(row[17], out classCode))
                                    jobs.Add(GetClassName(classCode));
                            }
                            if (row.Count > 18 && !string.IsNullOrEmpty(row[18]) && row[18] != "0")
                            {
                                int classCode;
                                if (int.TryParse(row[18], out classCode))
                                    jobs.Add(GetClassName(classCode));
                            }
                            if (row.Count > 19 && !string.IsNullOrEmpty(row[19]) && row[19] != "0")
                            {
                                int classCode;
                                if (int.TryParse(row[19], out classCode))
                                    jobs.Add(GetClassName(classCode));
                            }
                            if (jobs.Count > 0)
                                levelAndReqs = string.Format(" [{0}]", string.Join(", ", jobs.ToArray()));
                            
                            // Level/Stat requirements
                            if (row.Count > 20 && row.Count > 21)
                            {
                                int statID1, amt1;
                                if (int.TryParse(row[20], out statID1) && int.TryParse(row[21], out amt1) && statID1 > 0 && amt1 > 0)
                                {
                                    if (statID1 == 31)
                                    {
                                        levelAndReqs += string.Format(" [Lv.{0}]", amt1);
                                    }
                                    else
                                    {
                                        levelAndReqs += string.Format(" [Req: {0} {1}]", GetStatName(statID1), amt1);
                                    }
                                }
                                
                                if (row.Count > 22 && row.Count > 23)
                                {
                                    int statID2, amt2;
                                    if (int.TryParse(row[22], out statID2) && int.TryParse(row[23], out amt2) && statID2 > 0 && amt2 > 0)
                                    {
                                        levelAndReqs += string.Format(" [{0} {1}]", GetStatName(statID2), amt2);
                                    }
                                }
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(itemName))
                {
                    itemText.Inlines.Add(new System.Windows.Documents.Run(itemName + levelAndReqs));
                    itemText.Inlines.Add(new System.Windows.Documents.Run(string.Format(" (ID: {0})", itemID))
                    {
                        Foreground = Brushes.Gray
                    });
                }
                else
                {
                    itemText.Inlines.Add(new System.Windows.Documents.Run(string.Format("Item ID: {0}", itemID)));
                }
            }
            else
            {
                itemText.Inlines.Add(new System.Windows.Documents.Run("NULL")
                {
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic
                });
            }

            rowPanel.Children.Add(itemText);

            return rowPanel;
        }

        /// <summary>
        /// Handles the Click event of the Close button.
        /// </summary>
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (hasChanges)
            {
                var result = MessageBox.Show("You have unsaved changes. Do you want to save before closing?", 
                    "Unsaved Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    SaveChanges();
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }
            Close();
        }

        /// <summary>
        /// Adds a new drop table.
        /// </summary>
        private void AddDropTable_Click(object sender, RoutedEventArgs e)
        {
            if (!FileManager.STBs.ContainsKey("ITEM_DROP"))
                return;

            // Add a new empty row to the STB
            var newRow = new List<string>();
            FileManager.STBs["ITEM_DROP"].Cells.Add(newRow);

            hasChanges = true;
            
            // Set scroll target to the new row
            int newDropNumber = FileManager.STBs["ITEM_DROP"].Cells.Count - 1;
            scrollToDropNumber = newDropNumber;
            
            LoadDropTables();

            MessageBox.Show(string.Format("Drop Table {0} has been added.", newDropNumber), 
                "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Removes a drop table.
        /// </summary>
        private void RemoveDropTable_Click(int dropNumber)
        {
            var result = MessageBox.Show(string.Format("Are you sure you want to remove Drop Table {0}?\n\nWARNING: This will shift all drop table numbers after this one!", dropNumber), 
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            FileManager.STBs["ITEM_DROP"].Cells.RemoveAt(dropNumber);
            hasChanges = true;
            LoadDropTables();

            MessageBox.Show("Drop table removed. All subsequent drop table numbers have been shifted down by 1.", 
                "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Gets stat name from stat ID.
        /// </summary>
        private string GetStatName(int statID)
        {
            switch (statID)
            {
                case 10: return "STR";
                case 11: return "DEX";
                case 12: return "INT";
                case 13: return "CON";
                case 14: return "CHA";
                case 15: return "SEN";
                case 16: return "HP";
                case 17: return "MP";
                case 18: return "ATK";
                case 19: return "DEF";
                case 20: return "Hit Rate";
                case 21: return "M-Resist";
                case 22: return "Dodge";
                case 23: return "Move Speed";
                case 24: return "A-Speed";
                case 25: return "Bag Cap";
                case 26: return "Crit Rate";
                case 27: return "HP Recov";
                case 28: return "MP Recov";
                case 29: return "MP Cost";
                case 30: return "EXP";
                case 31: return "Level";
                case 37: return "Stat Points";
                case 38: return "Max HP";
                case 39: return "Max MP";
                case 40: return "Money";
                case 63: return "Passive Drop Rate";
                case 71: return "Drop Rate";
                case 76: return "Stamina";
                case 92: return "Clan Points";
                default: return statID.ToString();
            }
        }

        /// <summary>
        /// Gets item type name from type code.
        /// </summary>
        private string GetItemTypeName(int typeCode)
        {
            switch (typeCode)
            {
                // Face Items (11x)
                case 111: return "Mask";
                case 112: return "Glasses";
                case 113: return "Etc.";
                // Head Gear (12x)
                case 121: return "Helmet";
                case 122: return "Magic Hat";
                case 123: return "Hat";
                case 124: return "Hair Accessory";
                case 126: return "Mana Rune";
                // Body Armor (13x)
                case 131: return "Combat Uniform";
                case 132: return "Magic Clothes";
                case 133: return "Casual Clothes";
                // Gloves (14x)
                case 141: return "Gauntlet";
                case 142: return "Magic Glove";
                case 143: return "Glove";
                // Footwear (15x)
                case 151: return "Boots";
                case 152: return "Magic Boots";
                case 153: return "Shoes";
                // Back/Inventory (16x)
                case 161: return "Back Armor";
                case 162: return "Bag";
                case 163: return "Wings";
                case 164: return "Arrow Box";
                case 165: return "Bullet Box";
                case 166: return "Shell Box";
                // Accessories (17x)
                case 171: return "Ring";
                case 172: return "Necklace";
                case 173: return "Earrings";
                // One-Handed Weapons (21x)
                case 211: return "One-Handed Sword";
                case 212: return "One-Handed Blunt Weapon";
                // Two-Handed Weapons (22x)
                case 221: return "Two-Handed Sword";
                case 222: return "Spear";
                case 223: return "Two-Handed Axe";
                // Ranged Weapons (23x)
                case 231: return "Bow";
                case 232: return "Gun";
                case 233: return "Launcher";
                // Magic Weapons (24x)
                case 241: return "Magic Staff";
                case 242: return "Magic Wand";
                // Dual Weapons (25x)
                case 251: return "Katar";
                case 252: return "Dual Swords";
                case 253: return "Dual Guns";
                // Sub-Weapons/Shields (26x)
                case 261: return "Shield";
                case 262: return "Support Tool";
                case 263: return "Dolls";
                case 264: return "Magic Shield";
                case 265: return "Magic Books";
                // Special Weapons (27x)
                case 271: return "Crossbow";
                // Consumables (31x)
                case 311: return "Medicine";
                case 312: return "Food";
                case 313: return "Magic Item";
                case 314: return "Skill Book";
                case 315: return "Repair Tool";
                case 316: return "Special Scroll";
                case 317: return "Engine Fuel";
                // Special Items (32x)
                case 320: return "Automatic Consumption";
                case 321: return "Time Coupon";
                case 322: return "Random Box";
                case 323: return "Remote Storage Scroll";
                case 324: return "Restat Hammer";
                case 325: return "Item Mall Box";
                // Event (33x)
                case 330: return "Event Item";
                // Currency (36x)
                case 363: return "Item Mall Coin";
                // Valuables (41x)
                case 411: return "Jewel";
                case 412: return "Work of Art";
                case 413: return "Chest Key";
                // Crafting Materials - Refined (42x)
                case 421: return "Metal";
                case 422: return "Otherworldly Metal";
                case 423: return "Stone Material";
                case 424: return "Wooden Material";
                case 425: return "Leather";
                case 426: return "Cloth";
                case 427: return "Refining Material";
                case 428: return "Chemicals";
                case 429: return "Material";
                // Crafting Materials - Raw (43x)
                case 430: return "Gathered Goods";
                case 431: return "Arrow";
                case 432: return "Bullet";
                case 433: return "Shell";
                case 435: return "Ore";
                case 436: return "Harvest Material";
                // Quest Items (44x)
                case 441: return "Quest Items";
                case 442: return "Certification";
                case 443: return "Savage Mount";
                case 444: return "Mount Certificate";
                // Cart Parts (50x-51x)
                case 500: return "Snowmobile Frame";
                case 501: return "Snowmobile Engine";
                case 502: return "Snowmobile Skis";
                case 507: return "HoverBike Body";
                case 508: return "HoverBike Engine";
                case 509: return "HoverBike Fueltank";
                case 510: return "Mount";
                case 511: return "Cart Body";
                case 512: return "Castle Gear Body";
                case 513: return "Mount";
                case 514: return "Air Craft Body";
                case 515: return "Wolf Saddle";
                case 516: return "Lion Saddle";
                case 517: return "Dragon Saddle";
                case 518: return "Tiger Saddle";
                case 519: return "Scavenger Saddle";
                // Cart Engines (52x)
                case 521: return "Cart Engine";
                case 522: return "Castle Gear Engine";
                case 523: return "Jelly Riddle Assembly";
                case 524: return "Air Craft Engine";
                case 525: return "Wolf Mount";
                case 526: return "Lion Mount";
                case 527: return "Dragon Mount";
                case 528: return "Tiger Mount";
                case 529: return "Scavenger Mount";
                // Cart Wheels/Legs (53x)
                case 531: return "Cart Wheels";
                case 532: return "Castle Gear Leg";
                case 533: return "Tamed JellyBean";
                case 534: return "Air Craft Wings";
                case 535: return "Wolf Reins";
                case 536: return "Lion Reins";
                case 537: return "Dragon Reins";
                case 538: return "Tiger Reins";
                case 539: return "Scavenger Reins";
                // Cart Weapons (55x)
                case 551: return "Cart Accessory";
                case 552: return "Castle Gear Weapon";
                case 553: return "Arme pour Choropy";
                case 554: return "Air Craft Weapon";
                case 555: return "Wolf Weapon";
                case 556: return "Lion Weapon";
                case 557: return "Dragon Weapon";
                case 558: return "Tiger Weapon";
                case 559: return "Scavenger Weapon";
                // Special Cart/Accessories (57x-58x)
                case 570: return "Armbracelet";
                case 571: return "Cart Blunt Weapon";
                case 572: return "Cart Magic Weapon";
                case 573: return "Cart Bow Weapon";
                case 574: return "Cart Bladed Weapon";
                case 575: return "Cart Gun Weapon";
                case 578: return "Title";
                case 579: return "Mirages";
                case 580: return "Glaives";
                // Mini-Cart & Special Items (58x-60x)
                case 581: return "Mini-Cart Frame";
                case 582: return "Mini-Cart Engine";
                case 583: return "Mini-Cart Wheels";
                case 584: return "Fusion Link";
                case 585: return "Fusion Ect";
                case 586: return "Fusion Crystal";
                case 587: return "PvP Object";
                case 588: return "PvP Token";
                case 589: return "Christmas Event Frame";
                case 590: return "Christmas Item Mall Frame";
                case 591: return "Meister Cart Frame";
                case 592: return "Christmas Event Engine";
                case 593: return "Christmas Item Mall Engine";
                case 594: return "Christmas Event Skis";
                case 595: return "Christmas Item Mall Wheels";
                case 596: return "Christmas Accessory";
                case 597: return "Meister Cart Engine";
                case 598: return "Meister Cart Wheels";
                case 599: return "Dungeon Cart Frame";
                case 600: return "Dungeon Cart Engine";
                case 601: return "Dungeon Cart Wheels";
                case 602: return "Experience Medal";
                default: return typeCode.ToString();
            }
        }

        /// <summary>
        /// Gets class name from class code.
        /// </summary>
        private string GetClassName(int classCode)
        {
            switch (classCode)
            {
                case 0: return "Visitor";
                case 11: return "Knight";
                case 12: return "Champ";
                case 13: return "Magician";
                case 14: return "Cleric";
                case 15: return "Raider";
                case 16: return "Scout";
                case 17: return "Bourgeois";
                case 18: return "Artisan";
                case 111: return "Soldier";
                case 211: return "Muse";
                case 311: return "Hawker";
                case 411: return "Dealer";
                case 121: return "Knight";
                case 122: return "Champ";
                case 221: return "Mage";
                case 222: return "Cleric";
                case 321: return "Raider";
                case 322: return "Scout";
                case 421: return "Bourg";
                case 422: return "Artisan";
                case 41: return "Soldier";
                case 42: return "Muse";
                case 43: return "Hawker";
                case 44: return "Dealer";
                case 46: return "1st Job";
                case 47: return "2nd Job";
                case 48: return "3rd Job";
                case 51: return "Soldier 2nd";
                case 52: return "Muse 2nd";
                case 53: return "Hawker 2nd";
                case 54: return "Dealer 2nd";
                case 56: return "Soldier 3rd";
                case 57: return "Muse 3rd";
                case 58: return "Hawker 3rd";
                case 59: return "Dealer 3rd";
                case 61: return "Knight";
                case 62: return "Champ";
                case 63: return "Mage";
                case 64: return "Cleric";
                case 65: return "Raider";
                case 66: return "Scout";
                case 67: return "Bourg";
                case 68: return "Artisan";
                case 71: return "Sol+Hawk";
                case 72: return "Non-Muse";
                default: return classCode.ToString();
            }
        }

        /// <summary>
        /// Gets detailed item info for display.
        /// </summary>
        private string GetItemDetails(int category, int index)
        {
            if (!ItemCategories.ContainsKey(category))
                return "";

            string stbKey = ItemCategories[category];
            if (!FileManager.STBs.ContainsKey(stbKey))
                return "";

            var stb = FileManager.STBs[stbKey];
            if (index >= stb.Cells.Count || index < 0)
                return "";

            var row = stb.Cells[index];
            var details = new System.Text.StringBuilder();

            // Equipment categories (1-9): Armor, weapons, shields
            if (category >= 1 && category <= 9)
            {
                // Defense (col 32)
                if (row.Count > 32 && !string.IsNullOrEmpty(row[32]) && row[32] != "0")
                    details.AppendFormat("DEF: {0} | ", row[32]);

                // Magic Resist (col 33)
                if (row.Count > 33 && !string.IsNullOrEmpty(row[33]) && row[33] != "0")
                    details.AppendFormat("M-Resist: {0} | ", row[33]);

                // Attack (col 36 for weapons)
                if (category == 8 && row.Count > 36 && !string.IsNullOrEmpty(row[36]) && row[36] != "0")
                    details.AppendFormat("ATK: {0} | ", row[36]);

                // Attack Speed (col 37 for weapons)
                if (category == 8 && row.Count > 37 && !string.IsNullOrEmpty(row[37]) && row[37] != "0")
                    details.AppendFormat("A-Speed: {0} | ", row[37]);

                // Movement Speed (col 34 for boots)
                if (category == 5 && row.Count > 34 && !string.IsNullOrEmpty(row[34]) && row[34] != "0")
                    details.AppendFormat("Move Speed: {0} | ", row[34]);

                // Bonus Stats (cols 25 and 28 for stat IDs, 26 and 29 for amounts)
                if (row.Count > 25 && row.Count > 26)
                {
                    int bonusStat1, bonusAmt1;
                    if (int.TryParse(row[25], out bonusStat1) && int.TryParse(row[26], out bonusAmt1) && bonusStat1 > 0)
                    {
                        if (bonusAmt1 > 0)
                            details.AppendFormat("+{0} {1} | ", bonusAmt1, GetStatName(bonusStat1));
                        else if (bonusAmt1 < 0)
                            details.AppendFormat("{0} {1} | ", bonusAmt1, GetStatName(bonusStat1));
                    }
                }
                if (row.Count > 28 && row.Count > 29)
                {
                    int bonusStat2, bonusAmt2;
                    if (int.TryParse(row[28], out bonusStat2) && int.TryParse(row[29], out bonusAmt2) && bonusStat2 > 0)
                    {
                        if (bonusAmt2 > 0)
                            details.AppendFormat("+{0} {1} | ", bonusAmt2, GetStatName(bonusStat2));
                        else if (bonusAmt2 < 0)
                            details.AppendFormat("{0} {1} | ", bonusAmt2, GetStatName(bonusStat2));
                    }
                }
            }
            // Consumables (category 10): Required Stat 1, Bonus Stat 1
            else if (category == 10)
            {
                // Required Stat 1 (col 18), Amount 1 (col 19)
                if (row.Count > 18 && row.Count > 19)
                {
                    int reqStat, reqAmt;
                    if (int.TryParse(row[18], out reqStat) && int.TryParse(row[19], out reqAmt) && reqStat > 0 && reqAmt > 0)
                    {
                        details.AppendFormat("Req: {0} {1} | ", GetStatName(reqStat), reqAmt);
                    }
                }
                
                // Bonus Stat 1 (col 20), Amount 1 (col 21)
                if (row.Count > 20 && row.Count > 21)
                {
                    int bonusStat, bonusAmt;
                    if (int.TryParse(row[20], out bonusStat) && int.TryParse(row[21], out bonusAmt) && bonusStat > 0)
                    {
                        if (bonusAmt > 0)
                            details.AppendFormat("+{0} {1} | ", bonusAmt, GetStatName(bonusStat));
                        else if (bonusAmt < 0)
                            details.AppendFormat("{0} {1} | ", bonusAmt, GetStatName(bonusStat));
                    }
                }
            }
            // Gems (category 11): Bonus Stat 1, Bonus Stat 2
            else if (category == 11)
            {
                // Bonus Stat 1 (col 17), Amount 1 (col 18)
                if (row.Count > 17 && row.Count > 18)
                {
                    int bonusStat1, bonusAmt1;
                    if (int.TryParse(row[17], out bonusStat1) && int.TryParse(row[18], out bonusAmt1) && bonusStat1 > 0)
                    {
                        if (bonusAmt1 > 0)
                            details.AppendFormat("+{0} {1} | ", bonusAmt1, GetStatName(bonusStat1));
                        else if (bonusAmt1 < 0)
                            details.AppendFormat("{0} {1} | ", bonusAmt1, GetStatName(bonusStat1));
                    }
                }
                
                // Bonus Stat 2 (col 19), Amount 2 (col 20)
                if (row.Count > 19 && row.Count > 20)
                {
                    int bonusStat2, bonusAmt2;
                    if (int.TryParse(row[19], out bonusStat2) && int.TryParse(row[20], out bonusAmt2) && bonusStat2 > 0)
                    {
                        if (bonusAmt2 > 0)
                            details.AppendFormat("+{0} {1} | ", bonusAmt2, GetStatName(bonusStat2));
                        else if (bonusAmt2 < 0)
                            details.AppendFormat("{0} {1} | ", bonusAmt2, GetStatName(bonusStat2));
                    }
                }
            }
            // Materials/Ammo (category 12-13): Type, Quality
            else if (category == 12 || category == 13)
            {
                // Type (col 5)
                if (row.Count > 5 && !string.IsNullOrEmpty(row[5]) && row[5] != "0")
                {
                    int typeCode;
                    if (int.TryParse(row[5], out typeCode))
                    {
                        string typeName = GetItemTypeName(typeCode);
                        details.AppendFormat("Type: {0} | ", typeName);
                    }
                    else
                        details.AppendFormat("Type: {0} | ", row[5]);
                }
                
                // Quality (col 9)
                if (row.Count > 9 && !string.IsNullOrEmpty(row[9]) && row[9] != "0")
                    details.AppendFormat("Quality: {0} | ", row[9]);
            }
            // Cart/PAT parts (category 14): Movement Speed, Attack, Attack Speed
            else if (category == 14)
            {
                // Movement Speed (col 34)
                if (row.Count > 34 && !string.IsNullOrEmpty(row[34]) && row[34] != "0")
                    details.AppendFormat("Move Speed: {0} | ", row[34]);
                
                // Attack (col 37)
                if (row.Count > 37 && !string.IsNullOrEmpty(row[37]) && row[37] != "0")
                    details.AppendFormat("ATK: {0} | ", row[37]);
                
                // Attack Speed (col 38)
                if (row.Count > 38 && !string.IsNullOrEmpty(row[38]) && row[38] != "0")
                    details.AppendFormat("A-Speed: {0} | ", row[38]);
                
                // Bonus Stats (cols 25 and 28 for stat IDs, 26 and 29 for amounts)
                if (row.Count > 25 && row.Count > 26)
                {
                    int bonusStat1, bonusAmt1;
                    if (int.TryParse(row[25], out bonusStat1) && int.TryParse(row[26], out bonusAmt1) && bonusStat1 > 0)
                    {
                        if (bonusAmt1 > 0)
                            details.AppendFormat("+{0} {1} | ", bonusAmt1, GetStatName(bonusStat1));
                        else if (bonusAmt1 < 0)
                            details.AppendFormat("{0} {1} | ", bonusAmt1, GetStatName(bonusStat1));
                    }
                }
                if (row.Count > 28 && row.Count > 29)
                {
                    int bonusStat2, bonusAmt2;
                    if (int.TryParse(row[28], out bonusStat2) && int.TryParse(row[29], out bonusAmt2) && bonusStat2 > 0)
                    {
                        if (bonusAmt2 > 0)
                            details.AppendFormat("+{0} {1} | ", bonusAmt2, GetStatName(bonusStat2));
                        else if (bonusAmt2 < 0)
                            details.AppendFormat("{0} {1} | ", bonusAmt2, GetStatName(bonusStat2));
                    }
                }
            }

            string result = details.ToString();
            return result.EndsWith(" | ") ? result.Substring(0, result.Length - 3) : result;
        }

        /// <summary>
        /// Edits an item in a drop table.
        /// </summary>
        private void EditItem_Click(int dropNumber, int columnIndex, int currentItemID)
        {
            var input = new Window()
            {
                Title = string.Format("Edit Column {0}", columnIndex),
                Width = 600,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var mainGrid = new Grid() { Margin = new Thickness(10) };
            mainGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // Search
            mainGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // Filters
            mainGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // Category
            mainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) }); // Item list
            mainGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // Selected item
            mainGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // Buttons

            // Search box
            var searchPanel = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            var searchLabel = new Label() { Content = "Search:", Width = 80, Foreground = Brushes.Black };
            var searchBox = new TextBox() { Width = 300, Margin = new Thickness(0, 0, 5, 0) };
            var searchButton = new Button() { Content = "Search", Width = 70, Height = 22 };
            var clearButton = new Button() { Content = "Clear", Width = 70, Height = 22, Margin = new Thickness(5, 0, 0, 0) };
            
            searchPanel.Children.Add(searchLabel);
            searchPanel.Children.Add(searchBox);
            searchPanel.Children.Add(searchButton);
            searchPanel.Children.Add(clearButton);
            Grid.SetRow(searchPanel, 0);
            mainGrid.Children.Add(searchPanel);
            
            // Filter panel
            var filterPanel = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            var filterLabel = new Label() { Content = "Filters:", Width = 80, Foreground = Brushes.Black };
            
            var minLevelLabel = new Label() { Content = "Min Lv:", Foreground = Brushes.Black, Margin = new Thickness(0, 0, 5, 0) };
            var minLevelBox = new TextBox() { Width = 50, Margin = new Thickness(0, 0, 10, 0) };
            
            var maxLevelLabel = new Label() { Content = "Max Lv:", Foreground = Brushes.Black, Margin = new Thickness(0, 0, 5, 0) };
            var maxLevelBox = new TextBox() { Width = 50, Margin = new Thickness(0, 0, 10, 0) };
            
            var jobLabel = new Label() { Content = "Job:", Foreground = Brushes.Black, Margin = new Thickness(0, 0, 5, 0) };
            var jobCombo = new ComboBox() { Width = 120 };
            jobCombo.Items.Add(new ComboBoxItem() { Content = "All Jobs", Tag = -1 });
            jobCombo.Items.Add(new ComboBoxItem() { Content = "No Job", Tag = -2 });
            jobCombo.Items.Add(new ComboBoxItem() { Content = "Soldier", Tag = 111 });
            jobCombo.Items.Add(new ComboBoxItem() { Content = "Muse", Tag = 211 });
            jobCombo.Items.Add(new ComboBoxItem() { Content = "Hawker", Tag = 311 });
            jobCombo.Items.Add(new ComboBoxItem() { Content = "Dealer", Tag = 411 });
            jobCombo.SelectedIndex = 0;
            
            filterPanel.Children.Add(filterLabel);
            filterPanel.Children.Add(minLevelLabel);
            filterPanel.Children.Add(minLevelBox);
            filterPanel.Children.Add(maxLevelLabel);
            filterPanel.Children.Add(maxLevelBox);
            filterPanel.Children.Add(jobLabel);
            filterPanel.Children.Add(jobCombo);
            Grid.SetRow(filterPanel, 1);
            mainGrid.Children.Add(filterPanel);

            // Category and Sort panel
            var categoryPanel = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            var categoryLabel = new Label() { Content = "Category:", Width = 80, Foreground = Brushes.Black };
            var categoryCombo = new ComboBox() { Width = 200, Margin = new Thickness(0, 0, 10, 0) };
            
            var sortLabel = new Label() { Content = "Sort By:", Foreground = Brushes.Black, Margin = new Thickness(0, 0, 5, 0) };
            var sortCombo = new ComboBox() { Width = 150 };
            sortCombo.Items.Add(new ComboBoxItem() { Content = "ID (Ascending)", Tag = "id_asc" });
            sortCombo.Items.Add(new ComboBoxItem() { Content = "ID (Descending)", Tag = "id_desc" });
            sortCombo.Items.Add(new ComboBoxItem() { Content = "Name (A-Z)", Tag = "name_asc" });
            sortCombo.Items.Add(new ComboBoxItem() { Content = "Name (Z-A)", Tag = "name_desc" });
            sortCombo.Items.Add(new ComboBoxItem() { Content = "Level (Low-High)", Tag = "level_asc" });
            sortCombo.Items.Add(new ComboBoxItem() { Content = "Level (High-Low)", Tag = "level_desc" });
            sortCombo.SelectedIndex = 0;
            
            categoryCombo.Items.Add(new ComboBoxItem() { Content = "All Categories", Tag = 0 });
            categoryCombo.Items.Add(new ComboBoxItem() { Content = "1 - Mask", Tag = 1 });
            categoryCombo.Items.Add(new ComboBoxItem() { Content = "2 - Helmet", Tag = 2 });
            categoryCombo.Items.Add(new ComboBoxItem() { Content = "3 - Armor", Tag = 3 });
            categoryCombo.Items.Add(new ComboBoxItem() { Content = "4 - Glove", Tag = 4 });
            categoryCombo.Items.Add(new ComboBoxItem() { Content = "5 - Boot", Tag = 5 });
            categoryCombo.Items.Add(new ComboBoxItem() { Content = "6 - Back/Wings", Tag = 6 });
            categoryCombo.Items.Add(new ComboBoxItem() { Content = "7 - Jewelry", Tag = 7 });
            categoryCombo.Items.Add(new ComboBoxItem() { Content = "8 - Weapon", Tag = 8 });
            categoryCombo.Items.Add(new ComboBoxItem() { Content = "9 - Shield", Tag = 9 });
            categoryCombo.Items.Add(new ComboBoxItem() { Content = "10 - Consumable", Tag = 10 });
            categoryCombo.Items.Add(new ComboBoxItem() { Content = "11 - Gem", Tag = 11 });
            categoryCombo.Items.Add(new ComboBoxItem() { Content = "12 - Material", Tag = 12 });
            categoryCombo.Items.Add(new ComboBoxItem() { Content = "13 - Ammo", Tag = 13 });
            categoryCombo.Items.Add(new ComboBoxItem() { Content = "14 - Cart Part", Tag = 14 });
            categoryCombo.SelectedIndex = 0;
            
            categoryPanel.Children.Add(categoryLabel);
            categoryPanel.Children.Add(categoryCombo);
            categoryPanel.Children.Add(sortLabel);
            categoryPanel.Children.Add(sortCombo);
            Grid.SetRow(categoryPanel, 2);
            mainGrid.Children.Add(categoryPanel);

            // Item list
            var itemList = new ListBox() { Margin = new Thickness(0, 0, 0, 10) };
            Grid.SetRow(itemList, 3);
            mainGrid.Children.Add(itemList);

            // Selected item display
            var selectedPanel = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            var selectedLabel = new Label() { Content = "Selected ID:", Foreground = Brushes.Black };
            var selectedIDBox = new TextBox() { Width = 100, Margin = new Thickness(5, 0, 0, 0), Text = currentItemID.ToString() };
            var clearSelectionButton = new Button() { Content = "Clear (Set to 0)", Width = 100, Height = 22, Margin = new Thickness(10, 0, 0, 0) };
            
            selectedPanel.Children.Add(selectedLabel);
            selectedPanel.Children.Add(selectedIDBox);
            selectedPanel.Children.Add(clearSelectionButton);
            Grid.SetRow(selectedPanel, 4);
            mainGrid.Children.Add(selectedPanel);

            // Buttons
            var buttonPanel = new StackPanel() 
            { 
                Orientation = Orientation.Horizontal, 
                HorizontalAlignment = HorizontalAlignment.Right 
            };
            Grid.SetRow(buttonPanel, 5);

            var okButton = new Button() { Content = "OK", Width = 70, Margin = new Thickness(0, 0, 5, 0) };
            var cancelButton = new Button() { Content = "Cancel", Width = 70 };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            mainGrid.Children.Add(buttonPanel);

            input.Content = mainGrid;

            // Helper function to populate items
            Action populateItems = () =>
            {
                itemList.Items.Clear();
                
                var selectedCategory = categoryCombo.SelectedItem as ComboBoxItem;
                if (selectedCategory == null) return;
                
                int categoryFilter = (int)selectedCategory.Tag;
                string searchText = searchBox.Text.ToLower().Trim();
                
                // Get filter values
                int minLevel = 0, maxLevel = int.MaxValue;
                int.TryParse(minLevelBox.Text, out minLevel);
                if (!int.TryParse(maxLevelBox.Text, out maxLevel))
                    maxLevel = int.MaxValue;
                
                var selectedJob = jobCombo.SelectedItem as ComboBoxItem;
                int jobFilter = selectedJob != null ? (int)selectedJob.Tag : -1;
                
                var selectedSort = sortCombo.SelectedItem as ComboBoxItem;
                string sortMode = selectedSort != null ? (string)selectedSort.Tag : "id_asc";
                
                // Add option to clear
                var clearItem = new TextBlock() 
                { 
                    Text = "0 - NULL (Clear this column)",
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic
                };
                itemList.Items.Add(clearItem);
                
                // Collect items first for sorting - using Dictionary to store item data
                var itemsData = new Dictionary<TextBlock, ItemData>();
                
                for (int cat = 1; cat <= 14; cat++)
                {
                    // Filter by category if not "All"
                    if (categoryFilter != 0 && cat != categoryFilter)
                        continue;
                    
                    if (!ItemCategories.ContainsKey(cat))
                        continue;
                    
                    string stbKey = ItemCategories[cat];
                    string stlKey = ItemCategorySTLs[cat];
                    
                    if (!FileManager.STBs.ContainsKey(stbKey) || !FileManager.STLs.ContainsKey(stlKey))
                        continue;
                    
                    var stb = FileManager.STBs[stbKey];
                    int nameColumn = (cat >= 1 && cat <= 6) || (cat >= 8 && cat <= 9) || cat == 14 ? 0 : 1;
                    
                    for (int index = 1; index < stb.Cells.Count; index++)
                    {
                        if (stb.Cells[index].Count <= nameColumn)
                            continue;
                        
                        string nameID = stb.Cells[index][nameColumn];
                        if (string.IsNullOrEmpty(nameID) || nameID == "0")
                            continue;
                        
                        string itemName = FileManager.STLs[stlKey].Search(nameID);
                        if (string.IsNullOrEmpty(itemName))
                            continue;
                        
                        int itemID = (cat * 1000) + index;
                        
                        // For ammo category, only show items with bullet type populated (col 18)
                        if (cat == 13)
                        {
                            if (stb.Cells[index].Count <= 18 || string.IsNullOrEmpty(stb.Cells[index][18]) || stb.Cells[index][18] == "0")
                                continue;
                        }
                        
                        // Apply search filter
                        if (!string.IsNullOrEmpty(searchText))
                        {
                            if (!itemName.ToLower().Contains(searchText) && !itemID.ToString().Contains(searchText))
                                continue;
                        }
                        
                        // Get job, level and requirements for filtering and display
                        string levelAndReqs = "";
                        var row = stb.Cells[index];
                        int itemLevel = 0;
                        var itemJobs = new List<int>();
                        
                        if (cat >= 1 && cat <= 9)
                        {
                            // Job restrictions (cols 17-19)
                            var jobs = new List<string>();
                            if (row.Count > 17 && !string.IsNullOrEmpty(row[17]) && row[17] != "0")
                            {
                                int classCode;
                                if (int.TryParse(row[17], out classCode))
                                {
                                    jobs.Add(GetClassName(classCode));
                                    itemJobs.Add(classCode);
                                }
                            }
                            if (row.Count > 18 && !string.IsNullOrEmpty(row[18]) && row[18] != "0")
                            {
                                int classCode;
                                if (int.TryParse(row[18], out classCode))
                                {
                                    jobs.Add(GetClassName(classCode));
                                    itemJobs.Add(classCode);
                                }
                            }
                            if (row.Count > 19 && !string.IsNullOrEmpty(row[19]) && row[19] != "0")
                            {
                                int classCode;
                                if (int.TryParse(row[19], out classCode))
                                {
                                    jobs.Add(GetClassName(classCode));
                                    itemJobs.Add(classCode);
                                }
                            }
                            if (jobs.Count > 0)
                                levelAndReqs = string.Format(" [{0}]", string.Join(", ", jobs.ToArray()));
                            
                            // Level/Stat requirements
                            if (row.Count > 20 && row.Count > 21)
                            {
                                int statID1, amt1;
                                if (int.TryParse(row[20], out statID1) && int.TryParse(row[21], out amt1) && statID1 > 0 && amt1 > 0)
                                {
                                    if (statID1 == 31)
                                    {
                                        // Level requirement
                                        itemLevel = amt1;
                                        levelAndReqs += string.Format(" [Lv.{0}]", amt1);
                                    }
                                    else
                                    {
                                        // Other stat requirement
                                        levelAndReqs += string.Format(" [Req: {0} {1}]", GetStatName(statID1), amt1);
                                    }
                                }
                                
                                // Add second requirement if exists
                                if (row.Count > 22 && row.Count > 23)
                                {
                                    int statID2, amt2;
                                    if (int.TryParse(row[22], out statID2) && int.TryParse(row[23], out amt2) && statID2 > 0 && amt2 > 0)
                                    {
                                        levelAndReqs += string.Format(" [{0} {1}]", GetStatName(statID2), amt2);
                                    }
                                }
                            }
                        }
                        
                        // Apply filters
                        if (itemLevel < minLevel || itemLevel > maxLevel)
                            continue;
                        
                        // Job filter - strict filtering
                        if (jobFilter == -2)
                        {
                            // "No Job" selected - only show items with NO job restrictions
                            if (itemJobs.Count > 0)
                                continue;
                        }
                        else if (jobFilter != -1)
                        {
                            // Specific job selected - only show items for that job (must have job restrictions)
                            if (itemJobs.Count == 0)
                                continue; // Skip items with no job requirements
                            
                            bool canUse = false;
                            foreach (int jobCode in itemJobs)
                            {
                                // Check exact match or class-based match
                                if (jobCode == jobFilter || // Exact match (111, 211, 311, 411)
                                    jobCode == 0 || // Visitor (all can use)
                                    (jobFilter == 111 && (jobCode == 41 || jobCode == 51 || jobCode == 56 || jobCode == 121 || jobCode == 122 || jobCode == 61 || jobCode == 62)) || // Soldier classes
                                    (jobFilter == 211 && (jobCode == 42 || jobCode == 52 || jobCode == 57 || jobCode == 221 || jobCode == 222 || jobCode == 63 || jobCode == 64)) || // Muse classes
                                    (jobFilter == 311 && (jobCode == 43 || jobCode == 53 || jobCode == 58 || jobCode == 321 || jobCode == 322 || jobCode == 65 || jobCode == 66 || jobCode == 71)) || // Hawker classes
                                    (jobFilter == 411 && (jobCode == 44 || jobCode == 54 || jobCode == 59 || jobCode == 421 || jobCode == 422 || jobCode == 67 || jobCode == 68)) || // Dealer classes
                                    jobCode == 46 || jobCode == 47 || jobCode == 48) // Generic job level codes
                                {
                                    canUse = true;
                                    break;
                                }
                            }
                            if (!canUse)
                                continue;
                        }
                        // If jobFilter == -1 ("All Jobs"), show everything regardless of job restrictions
                        
                        // Get detailed info (without requirements since they're on the first line now)
                        string details = GetItemDetails(cat, index);
                        
                        // Create item display
                        var itemBlock = new TextBlock() { Margin = new Thickness(2) };
                        itemBlock.Inlines.Add(new System.Windows.Documents.Run(string.Format("{0} - ", itemID)) 
                        { 
                            FontWeight = FontWeights.Bold,
                            Foreground = Brushes.DarkBlue
                        });
                        itemBlock.Inlines.Add(new System.Windows.Documents.Run(itemName + levelAndReqs) 
                        { 
                            Foreground = Brushes.Black 
                        });
                        
                        if (!string.IsNullOrEmpty(details))
                        {
                            itemBlock.Inlines.Add(new System.Windows.Documents.Run("\n    " + details) 
                            { 
                                Foreground = Brushes.Gray,
                                FontSize = 10
                            });
                        }
                        
                        itemBlock.Tag = itemID;
                        itemsData[itemBlock] = new ItemData { ID = itemID, Name = itemName, Level = itemLevel };
                    }
                }
                
                // Sort items
                var sortedItems = new List<TextBlock>(itemsData.Keys);
                if (sortMode == "id_asc")
                    sortedItems.Sort((a, b) => itemsData[a].ID.CompareTo(itemsData[b].ID));
                else if (sortMode == "id_desc")
                    sortedItems.Sort((a, b) => itemsData[b].ID.CompareTo(itemsData[a].ID));
                else if (sortMode == "name_asc")
                    sortedItems.Sort((a, b) => string.Compare(itemsData[a].Name, itemsData[b].Name, StringComparison.OrdinalIgnoreCase));
                else if (sortMode == "name_desc")
                    sortedItems.Sort((a, b) => string.Compare(itemsData[b].Name, itemsData[a].Name, StringComparison.OrdinalIgnoreCase));
                else if (sortMode == "level_asc")
                    sortedItems.Sort((a, b) => itemsData[a].Level.CompareTo(itemsData[b].Level));
                else if (sortMode == "level_desc")
                    sortedItems.Sort((a, b) => itemsData[b].Level.CompareTo(itemsData[a].Level));
                
                // Add sorted items to list
                foreach (var item in sortedItems)
                {
                    itemList.Items.Add(item);
                }
            };

            // Event handlers
            searchButton.Click += (s, e) => populateItems();
            clearButton.Click += (s, e) => { searchBox.Text = ""; minLevelBox.Text = ""; maxLevelBox.Text = ""; jobCombo.SelectedIndex = 0; populateItems(); };
            categoryCombo.SelectionChanged += (s, e) => populateItems();
            sortCombo.SelectionChanged += (s, e) => populateItems();
            jobCombo.SelectionChanged += (s, e) => populateItems();
            
            minLevelBox.TextChanged += (s, e) => 
            {
                if (!string.IsNullOrEmpty(minLevelBox.Text))
                    populateItems();
            };
            
            maxLevelBox.TextChanged += (s, e) => 
            {
                if (!string.IsNullOrEmpty(maxLevelBox.Text))
                    populateItems();
            };
            
            searchBox.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                    populateItems();
            };
            
            itemList.SelectionChanged += (s, e) =>
            {
                if (itemList.SelectedItem != null)
                {
                    var textBlock = itemList.SelectedItem as TextBlock;
                    if (textBlock != null && textBlock.Tag != null)
                    {
                        selectedIDBox.Text = textBlock.Tag.ToString();
                    }
                    else if (itemList.SelectedItem.ToString().StartsWith("0 -"))
                    {
                        selectedIDBox.Text = "0";
                    }
                }
            };
            
            itemList.MouseDoubleClick += (s, e) =>
            {
                if (itemList.SelectedItem != null)
                {
                    okButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }
            };
            
            clearSelectionButton.Click += (s, e) => { selectedIDBox.Text = "0"; };

            okButton.Click += (s, e) =>
            {
                int itemID;
                if (int.TryParse(selectedIDBox.Text, out itemID) && itemID >= 0)
                {
                    FileManager.STBs["ITEM_DROP"].Cells[dropNumber][columnIndex] = itemID.ToString();
                    hasChanges = true;
                    
                    // Keep this drop table expanded
                    scrollToDropNumber = dropNumber;
                    
                    LoadDropTables();
                    input.Close();
                }
                else
                {
                    MessageBox.Show("Please enter a valid item ID (0 or greater).", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            cancelButton.Click += (s, e) => input.Close();

            // Initial population
            populateItems();
            
            input.ShowDialog();
        }

        /// <summary>
        /// Saves changes to the drop table file.
        /// </summary>
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveChanges();
        }

        /// <summary>
        /// Saves changes to the ITEM_DROP.STB file.
        /// </summary>
        private void SaveChanges()
        {
            if (!hasChanges)
            {
                MessageBox.Show("No changes to save.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                FileManager.STBs["ITEM_DROP"].Save();
                hasChanges = false;
                MessageBox.Show("Drop tables saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Error saving drop tables:\n{0}", ex.Message), 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
