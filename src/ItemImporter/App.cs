// Copyright (C) 2021 Donovan Sullivan
// GNU General Public License v3.0+ (see LICENSE or https://www.gnu.org/licenses/gpl-3.0.txt)

using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ValhallaLootList.ItemImporter.WarcraftDatabase;

namespace ValhallaLootList.ItemImporter
{
    internal class App
    {
        private readonly ILogger<App> _logger;
        private readonly TypedDataContext _wowContext;
        private readonly Config _config;
        private readonly List<ItemTemplate> _itemTemplates;
        private readonly List<SpellTemplate> _spellTemplates;

        public App(ILogger<App> logger, IOptions<Config> config, TypedDataContext wowContext)
        {
            _logger = logger;
            _wowContext = wowContext;
            _config = config.Value;
            _itemTemplates = new();
            _spellTemplates = new();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("App started");

            await LoadTablesIntoMemoryAsync(cancellationToken);

            var items = new List<SeedItem>();

            foreach (var itemId in await LoadItemsFromSeedInstancesAsync(cancellationToken))
            {
                _logger.LogDebug("Discovered Item #{itemId}", itemId);

                if (!items.Any(x => x.Id == itemId) && ParseItem(itemId, null) is { } item)
                {
                    items.Add(item);
                }

                if (_config.Tokens.TryGetValue(itemId, out var tokenRewards))
                {
                    foreach (var tokenRewardId in tokenRewards)
                    {
                        _logger.LogDebug("Discovered Item #{tokenRewardId} as a reward from token #{itemId}.", tokenRewardId, itemId);

                        if (items.Find(item => item.Id == tokenRewardId) is { } tokenReward)
                        {
                            tokenReward.RewardFromId = itemId;
                        }
                        else
                        {
                            tokenReward = ParseItem(tokenRewardId, itemId);
                            if (tokenReward is not null)
                            {
                                items.Add(tokenReward);
                            }
                        }
                    }
                }
            }

            _logger.LogInformation("Parsed {count} items. Saving to seed file.", items.Count);

            await SaveItemsSeedAsync(items, cancellationToken);

            _logger.LogDebug("App finished");
        }

        private async Task LoadTablesIntoMemoryAsync(CancellationToken cancellationToken)
        {
            _itemTemplates.Clear();
            _itemTemplates.AddRange(await _wowContext.ItemTemplates.AsNoTracking().ToListAsync(cancellationToken));

            _spellTemplates.Clear();
            _spellTemplates.AddRange(await _wowContext.SpellTemplates.AsNoTracking().ToListAsync(cancellationToken));
        }

        private async Task<IEnumerable<uint>> LoadItemsFromSeedInstancesAsync(CancellationToken cancellationToken)
        {
            using var fs = File.OpenRead(_config.SeedInstancesPath);
            var instances = await JsonSerializer.DeserializeAsync<List<SeedInstance>>(fs, cancellationToken: cancellationToken);
            Debug.Assert(instances?.Count > 0);
            return instances.SelectMany(i => i.Encounters).SelectMany(e => e.Items10.Concat(e.Items10H).Concat(e.Items25).Concat(e.Items25H)).Distinct().OrderBy(id => id);
        }

        private async Task SaveItemsSeedAsync(List<SeedItem> items, CancellationToken cancellationToken)
        {
            using var fs = File.Create(_config.SeedItemsPath);
            await JsonSerializer.SerializeAsync(fs, items, options: new() { WriteIndented = true }, cancellationToken: cancellationToken);
        }

        private SeedItem? ParseItem(uint id, uint? tokenId)
        {
            var itemTemplate = _itemTemplates.Find(x => x.Entry == id);

            if (itemTemplate is null)
            {
                _logger.LogWarning("Item with ID {id} was not found! Item will not be parsed.", id);
                return null;
            }

            if (itemTemplate.Quality != 4)
            {
                _logger.LogWarning("'{name}' ({id}) is not epic quality. Item will not be parsed.", itemTemplate.Name, id);
                return null;
            }

            var item = new SeedItem
            {
                Id = id,
                RewardFromId = tokenId,
                Name = itemTemplate.Name
            };

            switch (itemTemplate.InventoryType)
            {
                case 0:
                    break;
                case 1:
                    item.Slot = InventorySlot.Head;
                    goto case 99;
                case 2:
                    item.Slot = InventorySlot.Neck;
                    break;
                case 3:
                    item.Slot = InventorySlot.Shoulder;
                    goto case 99;
                case 4:
                    item.Slot = InventorySlot.Shirt;
                    break;
                case 5:
                    item.Slot = InventorySlot.Chest;
                    goto case 99;
                case 6:
                    item.Slot = InventorySlot.Waist;
                    goto case 99;
                case 7:
                    item.Slot = InventorySlot.Legs;
                    goto case 99;
                case 8:
                    item.Slot = InventorySlot.Feet;
                    goto case 99;
                case 9:
                    item.Slot = InventorySlot.Wrist;
                    goto case 99;
                case 10:
                    item.Slot = InventorySlot.Hands;
                    goto case 99;
                case 11:
                    item.Slot = InventorySlot.Finger;
                    break;
                case 12:
                    item.Slot = InventorySlot.Trinket;
                    break;
                case 13:
                    item.Slot = InventorySlot.OneHand;
                    goto case 98;
                case 14:
                    item.Slot = InventorySlot.OffHand;
                    item.Type = ItemType.Shield;
                    break;
                case 15:
                    item.Slot = InventorySlot.Ranged;
                    item.Type = ItemType.Bow;
                    break;
                case 16:
                    item.Slot = InventorySlot.Back;
                    break;
                case 17:
                    item.Slot = InventorySlot.TwoHand;
                    goto case 98;
                case 19:
                    item.Slot = InventorySlot.Tabard;
                    break;
                case 20:
                    item.Slot = InventorySlot.Chest;
                    goto case 99;
                case 21:
                    item.Slot = InventorySlot.MainHand;
                    goto case 98;
                case 22:
                    item.Slot = InventorySlot.OffHand;
                    goto case 98;
                case 23:
                    item.Slot = InventorySlot.OffHand;
                    break;
                case 25:
                    item.Slot = InventorySlot.Ranged;
                    item.Type = ItemType.Thrown;
                    break;
                case 26:
                    item.Slot = InventorySlot.Ranged;
                    item.Type = itemTemplate.Subclass switch
                    {
                        3 => ItemType.Gun,
                        18 => ItemType.Crossbow,
                        19 => ItemType.Wand,
                        _ => default
                    };
                    break;
                case 28:
                    item.Slot = InventorySlot.Ranged;
                    item.Type = itemTemplate.Subclass switch
                    {
                        7 => ItemType.Libram,
                        8 => ItemType.Idol,
                        9 => ItemType.Totem,
                        _ => default
                    };
                    break;
                case 98: // parse weapon type
                    item.Type = itemTemplate.Subclass switch
                    {
                        0 or 1 => ItemType.Axe,
                        4 or 5 => ItemType.Mace,
                        6 => ItemType.Polearm,
                        7 or 8 => ItemType.Sword,
                        10 => ItemType.Stave,
                        13 => ItemType.Fist,
                        15 => ItemType.Dagger,
                        _ => default
                    };
                    break;
                case 99: // parse armor type
                    item.Type = itemTemplate.Subclass switch
                    {
                        1 => ItemType.Cloth,
                        2 => ItemType.Leather,
                        3 => ItemType.Mail,
                        4 => ItemType.Plate,
                        _ => default
                    };
                    break;
                default:
                    _logger.LogWarning("'{name}' ({entry}) has an unexpected InventoryType value of {inventoryType}!", itemTemplate.Name, itemTemplate.Entry, itemTemplate.InventoryType);
                    break;
            }

            item.ItemLevel = itemTemplate.ItemLevel;

            ParsePrimaryStat(item, itemTemplate.StatType1, itemTemplate.StatValue1);
            ParsePrimaryStat(item, itemTemplate.StatType2, itemTemplate.StatValue2);
            ParsePrimaryStat(item, itemTemplate.StatType3, itemTemplate.StatValue3);
            ParsePrimaryStat(item, itemTemplate.StatType4, itemTemplate.StatValue4);
            ParsePrimaryStat(item, itemTemplate.StatType5, itemTemplate.StatValue5);
            ParsePrimaryStat(item, itemTemplate.StatType6, itemTemplate.StatValue6);
            ParsePrimaryStat(item, itemTemplate.StatType7, itemTemplate.StatValue7);
            ParsePrimaryStat(item, itemTemplate.StatType8, itemTemplate.StatValue8);
            ParsePrimaryStat(item, itemTemplate.StatType9, itemTemplate.StatValue9);
            ParsePrimaryStat(item, itemTemplate.StatType10, itemTemplate.StatValue10);

            ParseSpell(item, itemTemplate.Spellid1, itemTemplate.Spelltrigger1);
            ParseSpell(item, itemTemplate.Spellid2, itemTemplate.Spelltrigger2);
            ParseSpell(item, itemTemplate.Spellid3, itemTemplate.Spelltrigger3);
            ParseSpell(item, itemTemplate.Spellid4, itemTemplate.Spelltrigger4);
            ParseSpell(item, itemTemplate.Spellid5, itemTemplate.Spelltrigger5);

            var classes = (Classes)itemTemplate.AllowableClass;

            // 1535 and 32767 are used on items that have no class restrictions. (They equal all class flags plus extra unknown flags)
            if (classes > 0 && classes != (Classes)1535 && classes != (Classes)32767)
            {
                const Classes allClasses =
                    Classes.Druid |
                    Classes.Hunter |
                    Classes.Mage |
                    Classes.Paladin |
                    Classes.Priest |
                    Classes.Rogue |
                    Classes.Shaman |
                    Classes.Warlock |
                    Classes.Warrior;

                item.UsableClasses = (classes & allClasses);
            }

            // 524288 flag = 'unique-equipped'
            item.IsUnique = itemTemplate.Maxcount == 1 || (itemTemplate.Flags & 524288U) != 0;

            item.QuestId = itemTemplate.Startquest;

            _logger.LogInformation("Finished parsing Item #{id}. '{name}' will be added.", id, item.Name);
            return item;
        }

        private void ParsePrimaryStat(SeedItem item, byte id, int value)
        {
            switch (id)
            {
                case 0:
                case 1:
                case 46: return;
                case 3: item.Agility = value; return;
                case 4: item.Strength = value; return;
                case 5: item.Intellect = value; return;
                case 6: item.Spirit = value; return;
                case 7: item.Stamina = value; return;
                case 12: item.Defense = value; return;
                case 13: item.Dodge = value; return;
                case 14: item.Parry = value; return;
                case 15: item.BlockRating = value; return;
                case 31: item.Hit = value; return;
                case 32: item.Crit = value; return;
                case 36: item.Haste = value; return;
                case 37: item.Expertise = value; return;
                case 38: item.AttackPower = value; return;
                case 43: item.ManaPer5 = value; return;
                case 44: item.ArmorPenetration = value; return;
                case 45: item.SpellPower = value; return;
                case 48: item.BlockValue = value; return;
                default:
                    _logger.LogWarning("'{itemName}' ({itemId}) has an unknown primary stat of {id}: {value}.", item.Name, item.Id, id, value);
                    return;
            }
        }

        private void ParseSpell(SeedItem item, uint spellId, int trigger)
        {
            if (spellId == 0)
            {
                return;
            }

            var spell = _spellTemplates.Find(spell => spell.Id == spellId);

            if (spell is null)
            {
                _logger.LogError("'{itemName}' ({itemId}) has an unknown spell #{spellId}!", item.Name, item.Id, spellId);
                return;
            }

            if (trigger == 0) // on-use
            {
                _logger.LogWarning("'{itemName}' ({itemId}) has an on-use effect that will prevent auto-determination!", item.Name, item.Id);
                item.HasOnUse = true;
            }
            if (trigger == 1) // passive
            {
                ParseSpellEffect(item, spell.EffectBasePoints1, spell.EffectApplyAuraName1, spell.EffectTriggerSpell1, spell.EffectMiscValue1);
                ParseSpellEffect(item, spell.EffectBasePoints2, spell.EffectApplyAuraName2, spell.EffectTriggerSpell2, spell.EffectMiscValue2);
                ParseSpellEffect(item, spell.EffectBasePoints3, spell.EffectApplyAuraName3, spell.EffectTriggerSpell3, spell.EffectMiscValue3);
            }
            else if (trigger == 2) // on-hit
            {
                _logger.LogWarning("'{itemName}' ({itemId}) has a proc effect that will prevent auto-determination!", item.Name, item.Id);
                item.HasProc = true;
            }
        }

        private void ParseSpellEffect(SeedItem item, int basePoints, uint auraName, uint triggerSpell, int miscValue)
        {
            if (triggerSpell > 0)
            {
                _logger.LogWarning("'{itemName}' ({itemId}) has a spell proc effect that will prevent auto-determination!", item.Name, item.Id);
                item.HasProc = true;
                return;
            }

            switch (auraName)
            {
                case 0: return;
                case 13:
                    if (miscValue == 126)
                    {
                        item.SpellPower = 1 + basePoints;
                    }
                    else
                    {
                        _logger.LogWarning("'{itemName}' ({itemId}) has a special effect that will prevent auto-determination!", item.Name, item.Id);
                        item.HasSpecial = true;
                    }
                    return;
                case 85: item.ManaPer5 = 1 + basePoints; return;
                case 99: item.AttackPower = 1 + basePoints; return;
                case 123:
                    if (miscValue is 1)
                    {
                        item.ArmorPenetration = Math.Abs(basePoints) - 2;
                    }
                    else if (miscValue is 124 or 4 or 16)
                    {
                        item.SpellPenetration = Math.Abs(basePoints) - 2;
                    }
                    else
                    {
                        _logger.LogWarning("'{itemName}' ({itemId}) has a special effect that will prevent auto-determination!", item.Name, item.Id);
                        item.HasSpecial = true;
                    }
                    return;
                case 124: item.AttackPower = 1 + basePoints; return;
                case 135:
                    if (miscValue == 126)
                    {
                        item.SpellPower = 1 + basePoints;
                    }
                    else
                    {
                        _logger.LogWarning("'{itemName}' ({itemId}) has a special effect that will prevent auto-determination!", item.Name, item.Id);
                        item.HasSpecial = true;
                    }
                    return;
                case 158: item.BlockValue += 1 + basePoints; return;
                case 4: // 38320 = improved seal of light
                case 107: // 38321 = improve healing touch
                case 108: // 37447 = improved mana gem
                case 112: // 37447 = improved mana gem
                case 234: // 35126 = silence resistance
                default:
                    item.HasSpecial = true;
                    _logger.LogWarning("'{itemName}' ({itemId}) has a special effect that will prevent auto-determination!", item.Name, item.Id);
                    return;
            }
        }
    }
}