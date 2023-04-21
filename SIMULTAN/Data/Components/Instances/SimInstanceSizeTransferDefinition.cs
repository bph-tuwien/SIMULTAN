using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// A collection of <see cref="SimInstanceSizeTransferDefinitionItem" /> for each axis
    /// </summary>
    public interface ISimInstanceSizeTransferDefinition : IEnumerable<SimInstanceSizeTransferDefinitionItem>
    {
        /// <summary>
        /// Returns the <see cref="SimInstanceSizeTransferDefinitionItem"/> for the given axis.
        /// </summary>
        /// <param name="index">The axis index</param>
        /// <returns>The <see cref="SimInstanceSizeTransferDefinitionItem"/> for this axis</returns>
        SimInstanceSizeTransferDefinitionItem this[SimInstanceSizeIndex index] { get; }

        /// <summary>
        /// Restores the references to other objects after loading
        /// </summary>
        /// <param name="instance">The instance this transfer definition belongs to</param>
        void RestoreReferences(SimComponentInstance instance);

        /// <summary>
        /// Creates a copy of the current object
        /// </summary>
        /// <returns>The copied object</returns>
        ISimInstanceSizeTransferDefinition Clone();
    }

    /// <summary>
    /// Implementation of the <see cref="ISimInstanceSizeTransferDefinition"/> interface.
    /// This class uses an interface to prevent direct access to setters from outside
    /// </summary>
    public class SimInstanceSizeTransferDefinition : ISimInstanceSizeTransferDefinition
    {
        private SimInstanceSizeTransferDefinitionItem[] items;

        /// <summary>
        /// Initializes a new instance of the SimInstanceSizeTransferDefinition class
        /// </summary>
        public SimInstanceSizeTransferDefinition()
        {
            this.items = new SimInstanceSizeTransferDefinitionItem[6]
            {
                new SimInstanceSizeTransferDefinitionItem(SimInstanceSizeTransferSource.User, null, 0.0),
                new SimInstanceSizeTransferDefinitionItem(SimInstanceSizeTransferSource.User, null, 0.0),
                new SimInstanceSizeTransferDefinitionItem(SimInstanceSizeTransferSource.User, null, 0.0),
                new SimInstanceSizeTransferDefinitionItem(SimInstanceSizeTransferSource.User, null, 0.0),
                new SimInstanceSizeTransferDefinitionItem(SimInstanceSizeTransferSource.User, null, 0.0),
                new SimInstanceSizeTransferDefinitionItem(SimInstanceSizeTransferSource.User, null, 0.0)
            };
        }
        /// <summary>
        /// Initializes a new instance of the InstanceSizeTransferDefinition class
        /// </summary>
        /// <param name="items">A list of <see cref="SimInstanceSizeTransferDefinitionItem"/> which should be used to initialize this class</param>
        public SimInstanceSizeTransferDefinition(IEnumerable<SimInstanceSizeTransferDefinitionItem> items)
        {
            this.items = new SimInstanceSizeTransferDefinitionItem[6];

            int idx = 0;
            foreach (var item in items)
            {
                this.items[idx] = item;
                idx++;
            }
        }

        /// <inheritdoc />
        public SimInstanceSizeTransferDefinitionItem this[SimInstanceSizeIndex index]
        {
            get
            {
                return items[(int)index];
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                items[(int)index] = value;
            }
        }

        /// <inheritdoc />
        public void RestoreReferences(SimComponentInstance instance)
        {
            foreach (var item in items)
            {
                item.RestoreReferences(instance.Component?.Factory?.ProjectData.IdGenerator, instance);
            }
        }

        /// <inheritdoc/>
        public ISimInstanceSizeTransferDefinition Clone()
        {
            return new SimInstanceSizeTransferDefinition(this.items.Select(x => x.Clone()));
        }

        /// <inheritdoc/>
        public IEnumerator<SimInstanceSizeTransferDefinitionItem> GetEnumerator()
        {
            return items.Cast<SimInstanceSizeTransferDefinitionItem>().GetEnumerator();
        }
        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return items.GetEnumerator();
        }
    }
}
