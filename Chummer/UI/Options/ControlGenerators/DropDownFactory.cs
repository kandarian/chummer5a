﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Chummer.Backend.Attributes.OptionAttributes;
using Chummer.Backend.Options;

namespace Chummer.UI.Options.ControlGenerators
{
    class DropDownFactory : IOptionWinFromControlFactory
    {
        //Probably going to give errors if display scaling is enabled
        private const int DOWN_ARRAY_SPACE = 20;
        private const int TEXTBOX_MATCH_LABEL = 4;

        public bool IsSupported(OptionItem backingEntry)
        {
            OptionEntryProxy v = backingEntry as OptionEntryProxy;
            if (v != null)
            {
                if (v.TargetProperty.PropertyType.IsEnum ||  v.TargetProperty.GetCustomAttribute<DropDownAttribute>() != null)
                    return true;
            }
            return false;
        }

        public Control Construct(OptionItem backingEntry)
        {

            OptionEntryProxy v = backingEntry as OptionEntryProxy;
            if (v != null)
            {

                ComboBox backing = new ComboBox()
                {
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                List<string> strings = new List<string>();
                if (v.TargetProperty.PropertyType.IsEnum)
                {
                    List<Enum> enums = new List<Enum>();
                    string name = v.TargetProperty.PropertyType.Name;
                    foreach (var value in Enum.GetValues(v.TargetProperty.PropertyType).Cast<Enum>())
                    {
                        string valueName = $"{name}_{value}";
                        enums.Add(value);
                        string display;
                        strings.Add(LanguageManager.Instance.TryGetString(valueName, out display)
                            ? display
                            : value.ToString());
                    }

                    // ReSharper disable once ObjectCreationAsStatement
                    new ComboBoxBinder<Enum>(v, backing, strings, enums);


                    backing.Width = strings.Select(s => TextRenderer.MeasureText(s, Control.DefaultFont).Width).Max() + DOWN_ARRAY_SPACE;
                    backing.Top -= TEXTBOX_MATCH_LABEL;
                    return backing;
                }
                DropDownAttribute attribute = v.TargetProperty.GetCustomAttribute<DropDownAttribute>();
                if (attribute != null)
                {
                    List<string> values = attribute.RealValues.ToList();
                    if (attribute.DirectDisplay != null)
                    {
                        strings.AddRange(attribute.DirectDisplay);   
                    }
                    else if (attribute.TranslatedDisplay != null)
                    {
                        strings.AddRange(attribute.TranslatedDisplay.Select(LanguageManager.Instance.GetString));
                    }
                    else
                    {
                        strings.AddRange(values);
                    }

                    new ComboBoxBinder<string>(v, backing, strings, values);
                    return backing;
                }



                //TODO: DropDownAttribute or something...


            }

            throw new NotImplementedException();
        }

        private class ComboBoxBinder<T>
        {
            private readonly OptionEntryProxy _backingField;
            private readonly ComboBox _uiElement;
            private readonly List<string> _displayValues;
            private readonly List<T> _realValues;

            public ComboBoxBinder(
                OptionEntryProxy backingField, 
                ComboBox uiElement, 
                List<string> displayValues,
                List<T> realValues)
            {
                _backingField = backingField;
                _uiElement = uiElement;
                _displayValues = displayValues;
                _realValues = realValues;
                uiElement.Items.AddRange(displayValues.ToArray());


                for (int i = 0; i < realValues.Count; i++)
                {
                    if (realValues[i].Equals(backingField.Value))
                        uiElement.SelectedIndex = i;
                }


                backingField.ValueChanged += BackingFieldOnValueChanged;
                uiElement.SelectedIndexChanged += UiElementOnSelectedIndexChanged;
            }

            private void UiElementOnSelectedIndexChanged(object sender, EventArgs eventArgs)
            {
                _backingField.Value = _realValues[_uiElement.SelectedIndex];
            }

            private void BackingFieldOnValueChanged()
            {
                int index = _realValues.IndexOf((T) _backingField.Value);
                _uiElement.SelectedIndex = index;
            }
        }
    }
}