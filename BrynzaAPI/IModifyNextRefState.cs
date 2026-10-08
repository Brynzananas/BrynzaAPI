using EntityStates;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrynzaAPI;
public interface IModifyNextRefState
{
    public abstract void ModifyNextRefState(ref EntityState entityState);
}
