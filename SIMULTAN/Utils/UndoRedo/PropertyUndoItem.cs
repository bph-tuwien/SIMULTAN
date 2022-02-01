using SIMULTAN.Exceptions;
using System;
using System.Diagnostics;
using System.Reflection;

namespace SIMULTAN.Utils.UndoRedo
{
    /// <summary>
    /// An IUndoItem for properties
    /// </summary>
    public class PropertyUndoItem : IUndoItem
    {
        private object target;
        private object newValue;
        private object oldValue;

        private PropertyInfo prop;

        /// <summary>
        /// Initializes a new instance of the PropertyUndoItem class
        /// </summary>
        /// <param name="target">The target object</param>
        /// <param name="property">The property name (for reflection)</param>
        /// <param name="value">The new value for this property</param>
        public PropertyUndoItem(object target, string property, object value)
        {
            this.target = target;
            this.newValue = value;

            if (target == null)
                throw new ArgumentNullException();

            this.prop = this.target.GetType().GetProperty(property);
            if (prop == null)
                throw new ArgumentException(String.Format("Property \"{0}\" is not a valid property of type {1}", property, target.GetType().FullName));

            if (value != null && !prop.PropertyType.IsAssignableFrom(value.GetType()))
                throw new ArgumentException(String.Format("Value type {0} does not match property type {1}", prop.PropertyType.FullName, value.GetType().FullName));
            if (value == null && Nullable.GetUnderlyingType(prop.PropertyType) != null)
                throw new ArgumentException(String.Format("Unable to assign null to type {0}", prop.PropertyType.FullName));

            this.oldValue = prop.GetValue(target);
        }

        /// <inheritdoc/>
        public UndoExecutionResult Execute()
        {
            try
            {
                Redo();
                return UndoExecutionResult.Executed;
            }
            catch (TargetInvocationException ex) when (ex.InnerException is PropertyUnsupportedValueException)
            {
                return UndoExecutionResult.Failed;
            }
        }

        /// <inheritdoc/>
        public void Redo()
        {
            prop.SetValue(target, newValue);
        }
        /// <inheritdoc/>
        public void Undo()
        {
            prop.SetValue(target, oldValue);
        }
    }
}
