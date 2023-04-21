using SIMULTAN.Data.Assets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.TestUtils
{
    internal class DummyResourceFileEntry : ResourceFileEntry
    {
        public override bool CanBeRenamed => false;

        public override bool CanBeMoved => false;


        public DummyResourceFileEntry(string path, int key) : base(null, Data.Users.SimUserRole.ADMINISTRATOR, "", true, key)
        {
            // Do not need to do anything right now
        }
    }
}
