using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Petaframework
{
    public static class TypeDef
    {
        public enum RequestMode
        {
            ServerSide,
            ClientSide
        }

        [Flags]
        public enum InputType
        {
            /// <summary>
            /// Sets it's not an input
            /// </summary>
            None = 1,
            /// <summary>
            /// Defines an Input that will also show in the listing
            /// </summary>
            WithListing = 2,
            /// <summary>
            /// Defines an Input but it's not show in the listing
            /// </summary>
            NoListing = 4,
            /// <summary>
            /// Defines an Input but it's only show in Edit Mode
            /// </summary>
            OnlyOnEdit = 8,
            /// <summary>
            /// Defines an Input that only insert options is enable in the Edit Mode
            /// </summary>
            OnlyOnEditForInsert = 16,
            /// <summary>
            /// Defines an Input that's only for listing
            /// </summary>
            OnlyForListing = 32,

            /// <summary>
            /// Defines an Input that show a List and expect one selection. This is associated with subforms.
            /// </summary>
            SelectOne = 64,

            /// <summary>
            /// Defines default property showed to user in SelectOne Input. This is associated with subforms and permits only one per class.
            /// </summary>
            SelectOneText = 128,

            /// <summary>
            /// Defines an read only Input
            /// </summary>
            ReadOnly = 256,

            /// <summary>
            /// Defines an Input with external treatment
            /// </summary>
            External = 512,

            /// <summary>
            /// Defines an Hiddend Input
            /// </summary>
            Hidden = 1024,

            /// <summary>
            /// Defines an Input that show a List and expect multiple selection.
            /// </summary>
            SelectMultiple = 2048
        }

        public enum Action
        {
            CREATE,
            READ,
            UPDATE,
            DELETE,
            LIST,
            _NONE
        }

        public static Action GetAction(String action)
        {
            if (string.IsNullOrWhiteSpace(action))
                return Action._NONE;
            switch (action.ToLower())
            {
                case Constants.FormAction.Create:
                    return Action.CREATE;
                case Constants.FormAction.Read:
                    return Action.READ;
                case Constants.FormAction.Update:
                    return Action.UPDATE;
                case Constants.FormAction.Delete:
                    return Action.DELETE;
                case Constants.FormAction.List:
                    return Action.LIST;
                default:
                    return Action._NONE;
            }
        }
        
        internal static InputType? GetEditFlags(Action action)
        {
            switch (action)
            {
                case Action.CREATE:
                    return InputType.NoListing | InputType.WithListing;
                case Action.READ:
                    break;
                case Action.UPDATE:
                    return InputType.NoListing | InputType.WithListing | InputType.OnlyOnEdit | InputType.OnlyOnEditForInsert;
                case Action.DELETE:
                    break;
                case Action.LIST:
                    break;
                case Action._NONE:
                    break;
                default:                    
                    break;
            }
            return null;
        }

        public static IEnumerable<Enum> GetUniqueFlags(this Enum flags)
        {
            ulong flag = 1;
            foreach (var value in Enum.GetValues(flags.GetType()).Cast<Enum>())
            {
                ulong bits = Convert.ToUInt64(value);
                while (flag < bits)
                {
                    flag <<= 1;
                }

                if (flag == bits && flags.HasFlag(value))
                {
                    yield return value;
                }
            }
        }
    }
}
