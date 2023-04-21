using SIMULTAN.Data.Components;
using SIMULTAN.Utils;
using System;
using System.Collections.Specialized;
using System.Linq;

namespace SIMULTAN.Data.MultiValues
{
    /// <summary>
    /// Points into a <see cref="SimMultiValueFunction"/> field. This pointer performs a lookup on a specific graph depending on an X-Axis position
    /// </summary>
    public sealed class SimMultiValueFunctionParameterSource : SimMultiValueParameterSource
    {
        private string graphName;
        private double axisValueX, axisValueY;

        /// <summary>
        /// The function field this pointer points to
        /// </summary>
        public SimMultiValueFunction Function { get; }

        /// <summary>
        /// The name of the graph on which the values are queried
        /// </summary>
        private SimMultiValueFunctionGraph Graph
        {
            get { return graph; }
            set
            {
                if (IsDisposed)
                    throw new InvalidOperationException("Unable to set graph on unsubscribed pointer");

                DetachEvents();

                graph = value;

                AttachEvents();
            }
        }
        private SimMultiValueFunctionGraph graph;

        /// <summary>
        /// The name of the graph on which the values are queried.
        /// This parameter has to exactly match the <see cref="SimMultiValueFunctionGraph.Name"/> property in order to find valid values.
        /// </summary>
        public string GraphName => graphName;
        /// <summary>
        /// The value on the X-Axis where the result is queried
        /// </summary>
        public double AxisValueX => axisValueX;
        /// <summary>
        /// The value on the Y-Axis which is used as a reference when the graph is not uniquely defined at the x position
        /// </summary>
        public double AxisValueY => axisValueY;

        /// <inheritdoc />
        public override SimMultiValue ValueField
        {
            get { return Function; }
        }

        /// <summary>
        /// Initializes a new instance of the MultiValueFunctionPointer class
        /// </summary>
        /// <param name="function">The function field</param>
        /// <param name="graphName">The name of the graph on which values are searched.
        /// This parameter has to exactly match the <see cref="SimMultiValueFunctionGraph.Name"/> property in order to find valid values.</param>
        /// <param name="axisValueX">The x-value for the lookup</param>
        /// <param name="axisValueY">The y reference value (used when the graph is not uniquely defined)</param>
        public SimMultiValueFunctionParameterSource(SimMultiValueFunction function, string graphName, double axisValueX, double axisValueY)
        {
            if (function == null)
                throw new ArgumentNullException(nameof(function));

            this.Function = function;

            this.graphName = graphName;
            this.axisValueX = axisValueX;
            this.axisValueY = axisValueY;

            this.Graph = function.Graphs.FirstOrDefault(x => x.Name == graphName);
            if (graph == null)
                Reset();

            RegisterParameter(ReservedParameters.MVF_OFFSET_X_FORMAT, function.UnitX);
        }


        /// <inheritdoc />
        public override object GetValue()
        {
            if (IsDisposed)
                throw new InvalidOperationException("You're trying to get the value of an unsubscribed value pointer");

            if (graph != null && !double.IsNaN(axisValueX) && !double.IsNaN(axisValueY))
            {
                double addX = 0.0;
                var paramX = GetValuePointerParameter(ReservedParameters.MVF_OFFSET_X_FORMAT);

                if (paramX != null)
                {

                    switch (paramX)
                    {
                        case SimDoubleParameter dParam:
                            addX = dParam.Value;
                            break;
                    }
                }
                return graph.GetValueFromX(axisValueX + addX, axisValueY);
            }

            return null;
        }
        /// <inheritdoc />
        public override SimParameterValueSource Clone()
        {
            return new SimMultiValueFunctionParameterSource(Function, graphName, axisValueX, axisValueY);
        }
        /// <inheritdoc />
        public override void SetFromParameters(double axisValueX, double axisValueY, double axisValueZ, string gs)
        {
            this.axisValueX = axisValueX;
            this.axisValueY = axisValueY;
            this.graphName = gs;
            this.Graph = this.Function.Graphs.FirstOrDefault(x => x.Name == gs);
            if (this.Graph == null)
                Reset();
        }
        /// <inheritdoc />
        public override bool IsSamePointer(SimMultiValueParameterSource other)
        {
            var o = other as SimMultiValueFunctionParameterSource;
            if (o != null)
            {
                return (o.ValueField == ValueField && graphName == o.graphName && axisValueX.EqualsWithNan(o.axisValueX)
                    && axisValueY.EqualsWithNan(o.axisValueY));
            }

            return false;
        }

        private void Reset()
        {
            this.Graph = null;
            this.graphName = null;
            this.axisValueX = double.NaN;
            this.axisValueY = double.NaN;
        }

        /// <inheritdoc />
        protected override void Dispose(bool isDisposing)
        {
            if (!IsDisposed && this.graph != null)
            {
                DetachEvents();
            }

            base.Dispose(isDisposing);
        }

        private void Graphs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var item in e.OldItems)
                {
                    if (((SimMultiValueFunctionGraph)item).Name == this.graphName)
                    {
                        Reset();
                        this.NotifyValueChanged();
                    }
                }
            }
        }

        private void Graph_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SimMultiValueFunctionGraph.Name))
            {
                this.graphName = Graph.Name;
                this.NotifyValueChanged();
            }
        }

        private void Graph_Points_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.NotifyValueChanged();
        }

        private bool isAttached = false;

        internal override void AttachEvents()
        {
            base.AttachEvents();

            if (!isAttached && !IsDisposed)
            {
                this.Function.Graphs.CollectionChanged += Graphs_CollectionChanged;

                if (Graph != null)
                {
                    this.Graph.Points.CollectionChanged += Graph_Points_CollectionChanged;
                    this.Graph.PropertyChanged += Graph_PropertyChanged;
                    this.Function.Deleting += this.Function_Deleting;
                }

                isAttached = true;
            }
        }

        private void Function_Deleting(object sender, EventArgs e)
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
                this.Function.Graphs.CollectionChanged -= Graphs_CollectionChanged;

                if (Graph != null)
                {
                    this.Graph.Points.CollectionChanged -= Graph_Points_CollectionChanged;
                    this.Graph.PropertyChanged -= Graph_PropertyChanged;
                    this.Function.Deleting -= this.Function_Deleting;
                }
            }
        }
    }
}
