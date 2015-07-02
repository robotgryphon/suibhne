using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ostenvighx.Suibhne.Attributes {

    [AttributeUsage(AttributeTargets.Class)]
    public class MessageHandlerAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public class UserJoinHandlerAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public class UserLeaveHandlerAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public class UserQuitHandlerAttribute : Attribute { }

}
