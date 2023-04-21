using SIMULTAN.Data.Components;
using SIMULTAN.Utils;
using System;
using System.Windows.Media.Media3D;
using static SIMULTAN.Data.MultiValues.SimMultiValueField3D;

namespace SIMULTAN.Data.MultiValues
{


    /// <summary>
    /// Value source for <see cref="SimMultiValueField3D"/> instances
    /// </summary>
    public sealed class SimMultiValueField3DParameterSource : SimMultiValueParameterSource
    {
        /// <summary>
        /// Stores the values along the axis. NOT the position in the value field
        /// </summary>
        double axisValueX, axisValueY, axisValueZ;

        /// <summary>
        /// Returns the Field3D this pointer is pointing to
        /// </summary>
        public SimMultiValueField3D Field { get; }

        /// <summary>
        /// Returns the value on the X-Axis (NOT the index/position)
        /// </summary>
        public double AxisValueX => axisValueX;
        /// <summary>
        /// Returns the value on the Y-Axis (NOT the index/position)
        /// </summary>
        public double AxisValueY => axisValueY;
        /// <summary>
        /// Returns the value on the Z-Axis (NOT the index/position)
        /// </summary>
        public double AxisValueZ => axisValueZ;

        /// <inheritdoc />
        public override SimMultiValue ValueField
        {
            get { return Field; }
        }

        /// <summary>
        /// Initializes a new instance of the SimMultiValueField3DPointer class
        /// </summary>
        /// <param name="field3D">The table</param>
        /// <param name="axisValueX">The value on the X-Axis (NOT the index/position)</param>
        /// <param name="axisValueY">The value on the Y-Axis (NOT the index/position)</param>
        /// <param name="axisValueZ">The value on the Z-Axis (NOT the index/position)</param>
        public SimMultiValueField3DParameterSource(SimMultiValueField3D field3D, double axisValueX, double axisValueY, double axisValueZ)
        {
            if (field3D == null)
                throw new ArgumentNullException(nameof(field3D));
            if (field3D.Factory == null)
                throw new ArgumentException("Field must be part of a project");

            this.Field = field3D;

            AttachEvents();
            this.axisValueX = axisValueX;
            this.axisValueY = axisValueY;
            this.axisValueZ = axisValueZ;

            RegisterParameter(ReservedParameters.MVT_OFFSET_X_FORMAT, field3D.UnitX);
            RegisterParameter(ReservedParameters.MVT_OFFSET_Y_FORMAT, field3D.UnitY);
            RegisterParameter(ReservedParameters.MVT_OFFSET_Z_FORMAT, field3D.UnitZ);
        }




        /// <inheritdoc />
        public override object GetValue()
        {
            if (IsDisposed)
                throw new InvalidOperationException("You're trying to get the value of an unsubscribed value pointer");

            if (double.IsNaN(axisValueX) || double.IsNaN(axisValueY) || double.IsNaN(axisValueZ))
                return double.NaN;

            var lookupPos = GetAxisLookupPosition();
            return Field.GetValue(lookupPos);
        }



        private Point3D GetAxisLookupPosition()
        {
            var pos = GetLookupPosition();

            Point3D lookupPos = new Point3D(
                Field.AxisPositionFromValue(Axis.X, pos.X),
                Field.AxisPositionFromValue(Axis.Y, pos.Y),
                Field.AxisPositionFromValue(Axis.Z, pos.Z)
                );

            return lookupPos;
        }

        private Point3D GetLookupPosition()
        {
            double addX = 0.0, addY = 0.0, addZ = 0.0;
            var paramX = GetValuePointerParameter(ReservedParameters.MVT_OFFSET_X_FORMAT);
            if (paramX != null && paramX is SimDoubleParameter xDouble)
                addX = xDouble.Value;

            var paramY = GetValuePointerParameter(ReservedParameters.MVT_OFFSET_Y_FORMAT);
            if (paramY != null && paramY is SimDoubleParameter yDouble)
                addY = yDouble.Value;

            var paramZ = GetValuePointerParameter(ReservedParameters.MVT_OFFSET_Z_FORMAT);
            if (paramZ != null && paramZ is SimDoubleParameter zDouble)
                addZ = zDouble.Value;

            Point3D lookupPos = new Point3D(
                axisValueX + addX,
                axisValueY + addY,
                axisValueZ + addZ
                );

            return lookupPos;
        }

        /// <inheritdoc />
        public override SimParameterValueSource Clone()
        {
            return new SimMultiValueField3DParameterSource(Field, axisValueX, axisValueY, axisValueZ);
        }

        /// <inheritdoc />
        public override void SetFromParameters(double axisValueX, double axisValueY, double axisValueZ, string gs)
        {
            this.axisValueX = axisValueX;
            this.axisValueY = axisValueY;
            this.axisValueZ = axisValueZ;
        }

        /// <inheritdoc />
        public override bool IsSamePointer(SimMultiValueParameterSource other)
        {
            var otherMyType = other as SimMultiValueField3DParameterSource;
            if (otherMyType != null)
            {
                if (Field == otherMyType.Field && axisValueX.EqualsWithNan(otherMyType.axisValueX) &&
                    axisValueY.EqualsWithNan(otherMyType.axisValueY) && axisValueZ.EqualsWithNan(otherMyType.axisValueZ))
                    return true;
            }

            return false;
        }

        /// <inheritdoc />
        protected override void Dispose(bool isDisposing)
        {
            if (!IsDisposed)
            {
                DetachEvents();
            }

            base.Dispose(isDisposing);
        }

        private void SetPosition(double x, double y, double z)
        {
            this.axisValueX = x;
            this.axisValueY = y;
            this.axisValueZ = z;
            NotifyValueChanged();
        }

        private void Table_ValueChanged(object sender, ValueChangedEventArgs args)
        {
            if (args.Range.Contains(GetLookupPosition()))
                NotifyValueChanged();
        }

        private void Table_AxisChanged(object sender, EventArgs e)
        {
            //Check if all axis are still in range
            bool isXValid = this.axisValueX >= Field.XAxis[0] && this.axisValueX <= Field.XAxis[Field.XAxis.Count - 1];
            bool isYValid = this.axisValueY >= Field.YAxis[0] && this.axisValueY <= Field.YAxis[Field.YAxis.Count - 1];
            bool isZValid = this.axisValueZ >= Field.ZAxis[0] && this.axisValueZ <= Field.ZAxis[Field.ZAxis.Count - 1];

            if (!isXValid || !isYValid || !isZValid)
            {
                SetPosition(double.NaN, double.NaN, double.NaN);
            }

            NotifyValueChanged();
        }


        private bool isAttached = false;

        internal override void AttachEvents()
        {
            base.AttachEvents();

            if (!isAttached && !IsDisposed)
            {
                this.Field.AxisChanged += Table_AxisChanged;
                this.Field.ValueChanged += Table_ValueChanged;
                this.Field.Deleting += Table_Deleting;

                isAttached = true;
            }
        }

        private void Table_Deleting(object sender, EventArgs e)
        {
            if (this.TargetParameter != null)
            {
                this.TargetParameter.ValueSource = null;
            }

        }

        internal override void DetachEvents()
        {
            base.DetachEvents();

            if (isAttached)
            {
                isAttached = false;
                this.Field.AxisChanged -= Table_AxisChanged;
                this.Field.ValueChanged -= Table_ValueChanged;
                this.Field.Deleting -= Table_Deleting;
            }
        }
    }
}
