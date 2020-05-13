using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace UnitTests
{
    [TestFixture]
    public class TransfersControllerTests
    {
        /*
         *  TODO:
         *  - Create transfer nonexistent wallet - 404/400;
         *  - Create transfer from nonexistent currency balance - 400;
         *  - Create transfer to nonexistent currency balance - 200 balance should be created;
         *  - Create transfer to magic currency - 400;
         *  - Confirm transfer to nonexistent currency balance - 500 or create balance;
         *  - Confirm deleted transfer - 404;
         *  - Confirm completed transfer - 200;
         *  - Confirm active transfer - 200;
         *  - Confirm transfer when amount of transfer grater than balance - 402;
         *  - Confirm transfer belongs to another wallet - 403;
         *  - Delete transfer belongs to another wallet - 403;
         *  - Delete nonexistent transfer - 404;
         *  - Delete completed transfer - 404;
         *  - Delete deleted earlier transfer - 200; 
         */
    }
}
