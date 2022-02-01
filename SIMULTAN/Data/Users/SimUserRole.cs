using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Users
{
    /// <summary>
    /// The different roles a user may have in the project
    /// </summary>
    public enum SimUserRole
    {
        /// <summary>
        /// An administrator. Has always read and write access on components 
        /// </summary>
        ADMINISTRATOR = 0,              // white        @
        /// <summary>
        /// A moderator
        /// </summary>
        MODERATOR = 1,                  // black        A
        /// <summary>
        /// The energy network operator
        /// </summary>
        ENERGY_NETWORK_OPERATOR = 2,    // dark orange  B
        /// <summary>
        /// The energy supplier
        /// </summary>
        ENERGY_SUPPLIER = 3,           // orange       C
        /// <summary>
        /// The building developer
        /// </summary>
        BUILDING_DEVELOPER = 4,         // dark green   D
        /// <summary>
        /// The building operator
        /// </summary>
        BUILDING_OPERATOR = 5,          // green        E
        /// <summary>
        /// The architect
        /// </summary>
        ARCHITECTURE = 6,               // light blue   F
        /// <summary>
        /// The fire safety inspector
        /// </summary>
        FIRE_SAFETY = 7,                // dark red     G
        /// <summary>
        /// The building physics expert. Probably the most important person in the project ;)
        /// </summary>
        BUILDING_PHYSICS = 8,           // blue         H
        /// <summary>
        /// The person responsible for the HVEC system
        /// </summary>
        MEP_HVAC = 9,                   // dark blue    I
        /// <summary>
        /// The process measuring controller
        /// </summary>
        PROCESS_MEASURING_CONTROL = 10, // darker blue  J
        /// <summary>
        /// The building contractor
        /// </summary>
        BUILDING_CONTRACTOR = 11,       // grey         K
        /// <summary>
        /// The guest user.
        /// </summary>
        GUEST = 12,       // white        L
    }
}
