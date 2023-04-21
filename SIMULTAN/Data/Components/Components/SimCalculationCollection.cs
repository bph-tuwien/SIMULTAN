using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SIMULTAN.Data.Components
{
    public partial class SimComponent
    {
        /// <summary>
        /// Stores a collection of calculations inside a single component.
        /// Sorts the entries such that dependencies are correctly handles when executing from first to last
        /// </summary>
        public class SimCalculationCollection : ObservableCollection<SimCalculation>
        {
            private SimComponent owner;

            /// <summary>
            /// Initializes a new instance of the CalculationCollection class
            /// </summary>
            /// <param name="owner">The component this collection belongs to</param>
            public SimCalculationCollection(SimComponent owner)
            {
                this.owner = owner;
            }


            #region Collection Implementation

            /// <inheritdoc />
            protected override void InsertItem(int index, SimCalculation item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));
                if (item.Factory != null)
                    throw new ArgumentException("item already belongs to a factory");

                this.owner.RecordWriteAccess();

                SetValues(item);
                base.InsertItem(index, item);
                Reorder();
            }
            /// <inheritdoc />
            protected override void RemoveItem(int index)
            {
                var oldItem = this[index];

                this.owner.RecordWriteAccess();
                UnsetValues(oldItem, owner.Factory?.ProjectData.IdGenerator);
                base.RemoveItem(index);
            }
            /// <inheritdoc />
            protected override void ClearItems()
            {
                this.owner.RecordWriteAccess();

                foreach (var item in this)
                    UnsetValues(item, owner.Factory?.ProjectData.IdGenerator);
                base.ClearItems();
            }
            /// <inheritdoc />
            protected override void SetItem(int index, SimCalculation item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));

                this.owner.RecordWriteAccess();

                var oldItem = this[index];
                UnsetValues(oldItem, owner.Factory?.ProjectData.IdGenerator);
                SetValues(item);
                base.SetItem(index, item);
                Reorder();
            }

            #endregion

            private void SetValues(SimCalculation item)
            {
                if (owner.Factory != null) //Ids are only possible when the component is already attached to a parent/factory.
                                           //If not the case, Id's have to be handed out when the component get's attached
                {
                    if (item.Id != SimId.Empty) //Use pre-stored id (only possible inside the same global location)
                    {
                        if (item.Id.GlobalId != Guid.Empty && item.Id.GlobalId != owner.Factory.CalledFromLocation.GlobalID)
                            throw new InvalidOperationException("Ids are not transferable between Factories. Please reset the Id before adding");

                        item.Id = new SimId(owner.Factory.CalledFromLocation, item.Id.LocalId);
                        owner.Factory.ProjectData.IdGenerator.Reserve(item, item.Id);
                    }
                    else
                        item.Id = owner.Factory.ProjectData.IdGenerator.NextId(item, owner.Factory.CalledFromLocation);

                    item.Factory = owner.Factory;
                }

                item.Component = owner;
            }
            private void UnsetValues(SimCalculation item, SimIdGenerator idGenerator)
            {
                idGenerator?.Remove(item);
                item.Id = new SimId(item.Id.GlobalId, item.Id.LocalId);
                item.Factory = null;
                item.Component = null;
            }

            internal void NotifyFactoryChanged(SimComponentCollection newValue, SimComponentCollection oldValue)
            {
                if (oldValue != null)
                {
                    foreach (var item in this)
                        UnsetValues(item, oldValue.ProjectData.IdGenerator);
                }

                if (newValue != null)
                {
                    foreach (var item in this)
                        SetValues(item);
                }
            }

            #region Utils

            private static List<SimCalculation> OrderCalculationsByDependency(IEnumerable<SimCalculation> calculations,
                            Func<SimCalculation, IEnumerable<KeyValuePair<string, SimDoubleParameter>>> inputSelector,
                            Func<SimCalculation, IEnumerable<KeyValuePair<string, SimDoubleParameter>>> returnSelector)
            {
                //Gather return parameters
                Dictionary<SimBaseParameter, List<SimCalculation>> returnParameters = new Dictionary<SimBaseParameter, List<SimCalculation>>();
                foreach (var calc in calculations)
                {
                    foreach (var returnParam in returnSelector(calc).Where(x => x.Value != null))
                    {
                        if (returnParameters.TryGetValue(returnParam.Value, out var paramCalcList))
                            paramCalcList.Add(calc);
                        else
                            returnParameters.Add(returnParam.Value, new List<SimCalculation> { calc });
                    }
                }

                HashSet<SimCalculation> remainingCalculations = calculations.ToHashSet();

                //Build dependency list
                Dictionary<SimCalculation, HashSet<SimCalculation>> dependencies = new Dictionary<SimCalculation, HashSet<SimCalculation>>();
                foreach (var calc in calculations)
                {
                    dependencies.Add(calc, inputSelector(calc).Where(x => x.Value != null && returnParameters.ContainsKey(x.Value))
                        .SelectMany(x => returnParameters[x.Value]).ToHashSet());
                }


                List<SimCalculation> result = new List<SimCalculation>();
                //Add all calculations where no input is contained in output
                while (dependencies.Count > 0)
                {
                    if (!dependencies.TryFirstOrDefault(x => x.Value.Count == 0, out var firstWithoutDependency))
                        return null;

                    result.Add(firstWithoutDependency.Key);

                    dependencies.Remove(firstWithoutDependency.Key);
                    dependencies.ForEach(x => x.Value.Remove(firstWithoutDependency.Key));
                }

                return result;
            }

            private void Reorder()
            {
                //Order calculations for dependencies
                var sortedList = OrderCalculationsByDependency(this,
                    x => x.InputParams, x => x.ReturnParams);

                if (sortedList != null)
                {
                    //Move items into correct locations
                    for (int i = 0; i < sortedList.Count; ++i)
                    {
                        Move(IndexOf(sortedList[i]), i);
                    }
                }
            }

            #endregion
        }
    }
}
