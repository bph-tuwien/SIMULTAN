using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data
{
    /// <summary>
    /// Generates and stores unique ids.
    /// The current implementation never resets Id's, but it uses longs so you can create quite a lot of them before it gets problematic
    /// </summary>
    public class SimIdGenerator
    {
        private long maxId = 0;
        internal long MaxId => maxId;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimIdGenerator" /> class
        /// </summary>
        public SimIdGenerator() { }

        private Dictionary<SimId, SimObjectNew> IdLookup { get; } = new Dictionary<SimId, SimObjectNew>();

        /// <summary>
        /// Returns a new and unused Id
        /// </summary>
        /// <param name="simObject">The object which should be associated with the Id</param>
        /// <param name="globalLocation">The global Id for the id</param>
        /// <returns>Returns a new id</returns>
        public SimId NextId(SimObjectNew simObject, IReferenceLocation globalLocation)
        {
            if (simObject == null)
                throw new ArgumentNullException(nameof(simObject));

            var id = new SimId(globalLocation, ++maxId);


            IdLookup.Add(id, simObject);
            return id;
        }

        /// <summary>
        /// Associates an object with a pre-existing id to the generator
        /// </summary>
        /// <param name="simObject">The object</param>
        /// <param name="id">The id for the object</param>
        public void Reserve(SimObjectNew simObject, SimId id)
        {
            if (simObject == null)
                throw new ArgumentNullException(nameof(simObject));

            maxId = Math.Max(maxId, id.LocalId);
            IdLookup.Add(id, simObject);
        }

        /// <summary>
        /// Removes an object from the generator
        /// </summary>
        /// <param name="simObject"></param>
        public void Remove(SimObjectNew simObject)
        {
            IdLookup.Remove(simObject.Id);
        }

        /// <summary>
        /// Sets the maximum id after loading. May only be called by the loader
        /// </summary>
        internal long LoaderMaxId { set { maxId = Math.Max(maxId, value); } }

        /// <summary>
        /// Resets the generator by clearing all lookup tables and resetting the <see cref="MaxId"/>
        /// </summary>
        public void Reset()
        {
            this.maxId = 0;
            this.IdLookup.Clear();
        }

        /// <summary>
        /// Returns the object which is associated with an Id
        /// </summary>
        /// <typeparam name="T">The type of the object</typeparam>
        /// <param name="id">The id</param>
        /// <returns>Either a valid object with the given Id or Null when the Id doesn't exist or when it has a different type</returns>
        public T GetById<T>(SimId id) where T : SimObjectNew
        {
            if (IdLookup.TryGetValue(id, out var result))
                return result as T;
            return null;
        }
    }
}
