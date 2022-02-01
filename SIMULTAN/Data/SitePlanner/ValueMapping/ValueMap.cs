using SIMULTAN.Data.MultiValues;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.SitePlanner
{
    /// <summary>
    /// EventHandler for the ValueMappingChanged event
    /// </summary>
    /// <param name="sender">The sender</param>
    public delegate void ValueMappingChangedEventHandler(object sender);

    /// <summary>
    /// Represents value mapping on SitePlanner projects.
    /// Contains a list of value mappings, i.e. associations of color maps and pre-filters to MultiValueBigTables.
    /// </summary>
    public class ValueMap : INotifyPropertyChanged
    {
        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Invoked when a parameter related to value mapping has changed
        /// </summary>
        public event ValueMappingChangedEventHandler ValueMappingChanged;

        /// <summary>
        /// Returns whether value mapping is enabled
        /// </summary>
        public bool IsEnabled
        {
            get => isEnabled;
            set
            {
                if (value != isEnabled)
                {
                    isEnabled = value;
                    OnValueMappingChanged();
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
                }
            }
        }
        private bool isEnabled = true;

        /// <summary>
        /// The currently active value mapping association
        /// </summary>
        public ValueMappingAssociation ActiveParametersAssociation
        {
            get => ActiveParametersAssociationIndex == -1 && ActiveParametersAssociationIndex < ParametersAssociations.Count ? null : ParametersAssociations[ActiveParametersAssociationIndex];
        }

        /// <summary>
        /// Index into ParametersAssociations of currenty active association, -1 if collection is empty
        /// </summary>
        public int ActiveParametersAssociationIndex
        {
            get => activeParametersAssociationIndex;
            set
            {
                if (activeParametersAssociationIndex != -1 && activeParametersAssociationIndex < ParametersAssociations.Count)
                    DetachParameterEventHandlers(ActiveParametersAssociation.Parameters);

                activeParametersAssociationIndex = value;

                if (activeParametersAssociationIndex != -1)
                    AttachParameterEventHandlers(ActiveParametersAssociation.Parameters);

                OnValueMappingChanged();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActiveParametersAssociationIndex)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActiveParametersAssociation)));
            }
        }
        private int activeParametersAssociationIndex = -1;

        /// <summary>
        /// List of all registered value mapping associations
        /// </summary>
        public ObservableCollection<ValueMappingAssociation> ParametersAssociations { get; private set; }
                    = new ObservableCollection<ValueMappingAssociation>();

        /// <summary>
        /// Initializes a new instance of this class
        /// </summary>
        public ValueMap()
        {
            ParametersAssociations.CollectionChanged += ParametersAssociations_CollectionChanged;
        }

        /// <summary>
        /// Connects to the MultiValueCollection to listen if a table is deleted that is contained in a value mapping association
        /// </summary>
        /// <param name="multiValueCollection"></param>
        public void EstablishValueFieldConnection(SimMultiValueCollection multiValueCollection)
        {
            multiValueCollection.CollectionChanged += ValueRecord_CollectionChanged;
        }

        private void ValueRecord_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                foreach (var t in e.OldItems)
                {
                    if (t is SimMultiValueBigTable table)
                    {
                        var affectedAssociations = ParametersAssociations.Where(x => x.Parameters.ValueTable == table).ToList();

                        foreach (var affected in affectedAssociations)
                        {
                            ParametersAssociations.Remove(affected);
                        }
                    }
                }
            }
        }

        private void ParametersAssociations_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                var affected = (ValueMappingAssociation)e.OldItems[0];
                DetachParameterEventHandlers(affected.Parameters);
            }
        }

        private void AttachParameterEventHandlers(ValueMappingParameters parameters)
        {
            parameters.PropertyChanged += Parameters_PropertyChanged;
            parameters.ValueTable.ValueChanged += MultiValueTable_ValueChanged;
            parameters.ValueTable.Resized += MultiValueTable_Resized;
            parameters.ValueToColorMap.Parameters.ColorMapParametersChanged += ColorMapParametersParameters_ColorMapParametersChanged;
            parameters.ValuePreFilter.Parameters.ValuePrefilterParametersChanged += PrefilterParameters_ValuePrefilterParametersChanged;
        }

        private void DetachParameterEventHandlers(ValueMappingParameters parameters)
        {
            parameters.PropertyChanged -= Parameters_PropertyChanged;
            parameters.ValueTable.ValueChanged -= MultiValueTable_ValueChanged;
            parameters.ValueTable.Resized -= MultiValueTable_Resized;
            parameters.ValueToColorMap.Parameters.ColorMapParametersChanged -= ColorMapParametersParameters_ColorMapParametersChanged;
            parameters.ValuePreFilter.Parameters.ValuePrefilterParametersChanged -= PrefilterParameters_ValuePrefilterParametersChanged;
        }

        private void OnValueMappingChanged()
        {
            ValueMappingChanged?.Invoke(this);
        }

        private void Parameters_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ValueMappingParameters parameters = (ValueMappingParameters)sender;
            if (e.PropertyName == nameof(ValueMappingParameters.ValueToColorMap))
            {
                parameters.RegisteredColorMaps.ForEach(x => x.Parameters.ColorMapParametersChanged -= ColorMapParametersParameters_ColorMapParametersChanged);
                parameters.ValueToColorMap.Parameters.ColorMapParametersChanged += ColorMapParametersParameters_ColorMapParametersChanged;
            }
            else if (e.PropertyName == nameof(ValueMappingParameters.ValuePreFilter))
            {
                parameters.RegisteredValuePrefilters.ForEach(x => x.Parameters.ValuePrefilterParametersChanged -= PrefilterParameters_ValuePrefilterParametersChanged);
                parameters.ValuePreFilter.Parameters.ValuePrefilterParametersChanged += PrefilterParameters_ValuePrefilterParametersChanged;
            }

            OnValueMappingChanged();
        }

        private void MultiValueTable_Resized(object sender, SimMultiValueBigTable.ResizeEventArgs e)
        {
            OnValueMappingChanged();
        }

        private void MultiValueTable_ValueChanged(object sender, SimMultiValueBigTable.ValueChangedEventArgs args)
        {
            OnValueMappingChanged();
        }

        private void ColorMapParametersParameters_ColorMapParametersChanged(object sender)
        {
            OnValueMappingChanged();
        }

        private void PrefilterParameters_ValuePrefilterParametersChanged(object sender)
        {
            OnValueMappingChanged();
        }
    }
}
