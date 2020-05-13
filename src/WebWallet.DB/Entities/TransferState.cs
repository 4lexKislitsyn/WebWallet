using System;
using System.Collections.Generic;
using System.Text;

namespace WebWallet.DB.Entities
{
    public enum  TransferState
    {
        Active = 0,
        Completed = 1,
        Deleted = -1,
    }
}
