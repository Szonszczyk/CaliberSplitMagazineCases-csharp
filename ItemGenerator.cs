using CaliberSplitMagazineCases.Interfaces;
using CaliberSplitMagazineCases.Loaders;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;


namespace CaliberSplitMagazineCases
{
    internal class ItemGenerator(
        ISptLogger<CaliberSplitMagazineCases> logger,
        DatabaseService databaseService,
        ConfigLoader configLoader,
        ModDatabaseLoader modDatabaseLoader,
        CustomItemCreator customItemCreator
    )
    {
        private readonly Dictionary<MongoId, TemplateItem> items = databaseService.GetItems();
        private bool SaveIDsDatabase = false;
        private CustomBarterConfig customBarterConfig = new();
        private readonly ConfigData modConfig = configLoader.Config;

        public void GenerateItems()
        {
            customBarterConfig = CreateCustomBarterConfig(modConfig, items, logger, "CaliberSplitMagazineCases");
            var magazines = LoadMagazines(modConfig, modDatabaseLoader);

            var itemCaseFilter = items["59fb042886f7746c5005a7b2"]?.Properties?.Grids?.FirstOrDefault()?.Properties?.Filters?.FirstOrDefault()?.Filter;
            var thiccItemCaseFilter = items["5c0a840b86f7742ffa4f2482"]?.Properties?.Grids?.FirstOrDefault()?.Properties?.Filters?.FirstOrDefault()?.Filter;

            foreach (var magazine in magazines)
            {
                var magazineType = magazine.Key;
                var magazineArray = magazine.Value;
                var knownAmmo = modDatabaseLoader.DbCaliberById.TryGetValue(magazineType, out CaliberInfo? value) ? value : new CaliberInfo { Name = magazineType, ShortName = magazineType };
                var newItem = new NewItemFromCloneDetails
                {
                    ItemTplToClone = "5c127c4486f7745625356c13",
                    ParentId = "5795f317245977243854e041",
                    HandbookParentId = "5b5f6fa186f77409407a7eb7",
                    NewId = ResolveMongoId(modDatabaseLoader, $"CASEID{magazineType}"),
                    FleaPriceRoubles = Math.Floor(modConfig.HandbookPriceRoubles * 1.3),
                    HandbookPriceRoubles = modConfig.HandbookPriceRoubles,
                    OverrideProperties = new TemplateItemProperties
                    {
                        BackgroundColor = IsPluginLoaded() ? modConfig.BackgroundColorColorConverterAPI : modConfig.BackgroundColor,
                        Weight = 0,
                        Width = modConfig.Width,
                        Height = modConfig.Height
                    },
                    Locales = new Dictionary<string, LocaleDetails>
                    {
                        {
                            "en", new LocaleDetails
                            {
                                Name = $"<b>Custom Magazine Case for {knownAmmo.Name} magazines</b>",
                                ShortName = $"{knownAmmo.ShortName} CMC",
                                Description = $"<align=\"center\">Custom magazine that can store all your <b>{knownAmmo.Name}</b> magazines!</align>"
                            }
                        }
                    }
                };
                Grid wholeCaseGrid = new()
                {
                    Id = ResolveMongoId(modDatabaseLoader, $"CASE{newItem.NewId}#AMMO:ALL#"),
                    Name = $"CASE:${newItem.NewId}#AMMO:ALL#",
                    Parent = newItem.NewId,
                    Prototype = "55d329c24bdc2d892f8b4567",
                    Properties = new()
                    {
                        CellsH = modConfig.CaseHeight,
                        CellsV = modConfig.CaseWidth,
                        Filters = [
                            new GridFilter {
                                Filter = magazineArray
                            }
                        ],
                        IsSortingTable = false,
                        MaxCount = 0,
                        MaxWeight = 0,
                        MinCount = 0
                    }
                };
                newItem.OverrideProperties.Grids = [wholeCaseGrid];
                var customItemConfig = new CustomItemConfig
                {
                    FleaBlacklisted = modConfig.FleaMarketBlacklisted
                };
                customItemCreator.AddItemToDatabase(newItem, customItemConfig, customBarterConfig);

                // Add case to filters of Item Case and THICC Item Case
                itemCaseFilter?.Add(newItem.NewId);
                thiccItemCaseFilter?.Add(newItem.NewId);
            }
            if (SaveIDsDatabase)
            {
                modDatabaseLoader.DbItemsIdsJsonSave();
            }
        }

        private Dictionary<string, HashSet<MongoId>> LoadMagazines(ConfigData config, ModDatabaseLoader modDatabaseLoader)
        {
            Dictionary<string, HashSet<MongoId>> magazines = [];

            foreach (TemplateItem item in items.Values)
            {
                if (item.Parent != "5448bc234bdc2d3c308b4569") continue;

                var filter = item?.Properties?.Cartridges?.FirstOrDefault()?.Properties?.Filters?.FirstOrDefault()?.Filter;

                if (item != null && filter != null)
                {
                    foreach(var ammo in filter)
                    {
                        var ammoCaliber = items[ammo]?.Properties?.Caliber;
                        if (ammoCaliber == null) continue;
                        if (config.UseOnlyKnownCalibers && !modDatabaseLoader.DbCaliberById.ContainsKey(ammoCaliber)) continue;
                        if (config.RemoveBadCalibers && config.BadCalibers.Contains(ammoCaliber)) continue;

                        if (!magazines.TryGetValue(ammoCaliber, out var list))
                        {
                            list = [];
                            magazines[ammoCaliber] = list;
                        }
                        if (!list.Contains(item.Id)) list.Add(item.Id);
                    }
                }
            }
            return magazines;
        }

        private string ResolveMongoId(ModDatabaseLoader modDatabaseLoader, string stringToMongoId)
        {
            if (!modDatabaseLoader.DbItemsIds.TryGetValue(stringToMongoId, out string? value))
            {
                SaveIDsDatabase = true;
                value = new MongoId();
                modDatabaseLoader.DbItemsIds.Add(stringToMongoId, value);
            }
            return value;
        }

        private static bool IsPluginLoaded()
        {
            const string pluginName = "rairai.colorconverterapi.dll";
            const string pluginsPath = "../BepInEx/plugins";

            try
            {
                if (!Directory.Exists(pluginsPath))
                    return false;

                var pluginList = Directory.GetFiles(pluginsPath)
                    .Select(System.IO.Path.GetFileName)
                    .Select(f => f.ToLowerInvariant());
                return pluginList.Contains(pluginName);
            }
            catch
            {
                return false;
            }
        }
        private static CustomBarterConfig CreateCustomBarterConfig(ConfigData config, Dictionary<MongoId, TemplateItem> items, ISptLogger<CaliberSplitMagazineCases> logger, string namespaceName)
        {
            if (config.CasesOnPeacekeeper)
            {
                return new CustomBarterConfig
                {
                    TraderId = Traders.PEACEKEEPER,
                    Price = config.USDPrice,
                    Barter = ItemTpl.MONEY_DOLLARS
                };
            }
            if (config.CasesOnRef)
            {
                return new CustomBarterConfig
                {
                    TraderId = Traders.REF,
                    Price = config.GpCoinPrice,
                    Barter = ItemTpl.MONEY_GP_COIN
                };
            }
            if (config.CasesOnSkier)
            {
                return new CustomBarterConfig
                {
                    TraderId = Traders.SKIER,
                    Price = config.EuroPrice,
                    Barter = ItemTpl.MONEY_EUROS
                };
            }
            if (config.CasesOnJaeger)
            {
                return new CustomBarterConfig
                {
                    TraderId = Traders.JAEGER,
                    Price = (int)Math.Floor(config.RoublesPriceMultiplier * config.HandbookPriceRoubles),
                    Barter = ItemTpl.MONEY_ROUBLES
                };
            }
            if (config.CasesOnPrapor)
            {
                if (MongoId.IsValidMongoId(config.BarterType) && items != null && items.TryGetValue(config.BarterType, out _))
                {
                    return new CustomBarterConfig
                    {
                        TraderId = Traders.PRAPOR,
                        Price = config.BarterPrice,
                        Barter = config.BarterType
                    };
                } else
                {
                    logger.LogWithColor($"[{namespaceName}] MongoId for Prapor barter: {config.BarterType} do not exists! Cases are added to Peacekeeper instead!", LogTextColor.Red);
                }
            }
            return new CustomBarterConfig
            {
                TraderId = "PEACEKEEPER",
                LoyalLevel = 1,
                Price = config.USDPrice,
                Barter = "DOLLARS"
            };
        }
    }
}
