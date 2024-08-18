using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace AGS.Types
{
    public class CustomPropertySchemaItem : ICloneable
    {
        private string _name;
        private string _description;
        private string _defaultValue;
        private CustomPropertyType _type;
        private CustomPropertyAppliesTo _appliesTo;

        public CustomPropertySchemaItem()
        {
            _name = string.Empty;
            _description = string.Empty;
            _defaultValue = string.Empty;
            _appliesTo = CustomPropertyAppliesTo.Everything;
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        public string DefaultValue
        {
            get { return _defaultValue; }
            set { _defaultValue = value; }
        }

        [AGSNoSerialize]
        public CustomPropertyAppliesTo AppliesTo
        {
            get { return _appliesTo; }
            set { _appliesTo = value; }
        }

        public bool AppliesToCharacters
        {
            get { return _appliesTo.HasFlag(CustomPropertyAppliesTo.Characters); }
            set { _appliesTo = value ? (_appliesTo | CustomPropertyAppliesTo.Characters) : (_appliesTo & ~CustomPropertyAppliesTo.Characters); }
        }

        public bool AppliesToHotspots
        {
            get { return _appliesTo.HasFlag(CustomPropertyAppliesTo.Hotspots); }
            set { _appliesTo = value ? (_appliesTo | CustomPropertyAppliesTo.Hotspots) : (_appliesTo & ~CustomPropertyAppliesTo.Hotspots); }
        }

        public bool AppliesToInvItems
        {
            get { return _appliesTo.HasFlag(CustomPropertyAppliesTo.InventoryItems); }
            set { _appliesTo = value ? (_appliesTo | CustomPropertyAppliesTo.InventoryItems) : (_appliesTo & ~CustomPropertyAppliesTo.InventoryItems); }
        }

        public bool AppliesToObjects
        {
            get { return _appliesTo.HasFlag(CustomPropertyAppliesTo.Objects); }
            set { _appliesTo = value ? (_appliesTo | CustomPropertyAppliesTo.Objects) : (_appliesTo & ~CustomPropertyAppliesTo.Objects); }
        }

        public bool AppliesToRooms
        {
            get { return _appliesTo.HasFlag(CustomPropertyAppliesTo.Rooms); }
            set { _appliesTo = value ? (_appliesTo | CustomPropertyAppliesTo.Rooms) : (_appliesTo & ~CustomPropertyAppliesTo.Rooms); }
        }

        public bool AppliesToAudioClips
        {
            get { return _appliesTo.HasFlag(CustomPropertyAppliesTo.AudioClips); }
            set { _appliesTo = value ? (_appliesTo | CustomPropertyAppliesTo.AudioClips) : (_appliesTo & ~CustomPropertyAppliesTo.AudioClips); }
        }

        public bool AppliesToDialogs
        {
            get { return _appliesTo.HasFlag(CustomPropertyAppliesTo.Dialogs); }
            set { _appliesTo = value ? (_appliesTo | CustomPropertyAppliesTo.Dialogs) : (_appliesTo & ~CustomPropertyAppliesTo.Dialogs); }
        }

        public bool AppliesToGUIs
        {
            get { return _appliesTo.HasFlag(CustomPropertyAppliesTo.GUIs); }
            set { _appliesTo = value ? (_appliesTo | CustomPropertyAppliesTo.GUIs) : (_appliesTo & ~CustomPropertyAppliesTo.GUIs); }
        }

        public bool AppliesToGUIControls
        {
            get { return _appliesTo.HasFlag(CustomPropertyAppliesTo.GUIControls); }
            set { _appliesTo = value ? (_appliesTo | CustomPropertyAppliesTo.GUIControls) : (_appliesTo & ~CustomPropertyAppliesTo.GUIControls); }
        }

        public bool AppliesToRegions
        {
            get { return _appliesTo.HasFlag(CustomPropertyAppliesTo.Regions); }
            set { _appliesTo = value ? (_appliesTo | CustomPropertyAppliesTo.Regions) : (_appliesTo & ~CustomPropertyAppliesTo.Regions); }
        }

        public bool AppliesToWalkableAreas
        {
            get { return _appliesTo.HasFlag(CustomPropertyAppliesTo.WalkableAreas); }
            set { _appliesTo = value ? (_appliesTo | CustomPropertyAppliesTo.WalkableAreas) : (_appliesTo & ~CustomPropertyAppliesTo.WalkableAreas); }
        }

        public CustomPropertyType Type
        {
            get { return _type; }
            set { _type = value; }
        }

        [AGSNoSerialize]
        public string AppliesToAsString
        {
            get
            {
                string toReturn = string.Empty;
                toReturn += AppliesToAudioClips ? "A" : "  ";
                toReturn += AppliesToCharacters ? "C" : "  ";
                toReturn += AppliesToDialogs ? "D" : "  ";
                toReturn += AppliesToGUIs ? "G" : "  ";
                toReturn += AppliesToGUIControls ? "Gc" : "  ";
                toReturn += AppliesToInvItems ? "I" : "  ";
                toReturn += AppliesToRooms ? "R" : "  ";
                toReturn += AppliesToHotspots ? "H" : "  ";
                toReturn += AppliesToObjects ? "O" : "  ";
                toReturn += AppliesToRegions ? "Rg" : "  ";
                toReturn += AppliesToWalkableAreas ? "W" : "  ";
                return toReturn;
            }
        }

        public object GetTypedDefaultValue()
        {
            if (_type == CustomPropertyType.Boolean)
            {
                if (_defaultValue == "1")
                {
                    return true;
                }
                return false;
            }
            if (_type == CustomPropertyType.Number)
            {
                int result = 0;
                Int32.TryParse(_defaultValue, out result);
                return result;
            }
            return _defaultValue;
        }

        public CustomPropertySchemaItem(XmlNode node)
        {
            this.AppliesTo = CustomPropertyAppliesTo.None; // reset before reading back
            SerializeUtils.DeserializeFromXML(this, node);
        }

        public void ToXml(XmlTextWriter writer)
        {
            SerializeUtils.SerializeToXML(this, writer);
        }

        public object Clone()
        {
            CustomPropertySchemaItem copy = new CustomPropertySchemaItem();
            copy.DefaultValue = this.DefaultValue;
            copy.Description = this.Description;
            copy.Name = this.Name;
            copy.Type = this.Type;
            copy.AppliesTo = this.AppliesTo;
            return copy;
        }
    }
}
