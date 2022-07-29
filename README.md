# SIMULTAN

The SIMULTAN data model for BIM data offers an Open-Source way to store information about buildings and structures.

The data model of SIMULTAN is capable of depicting an arbitrary data structure. It is free of semantics and offers abstract syntactic containers 
for any semantics that can be represented by a directed cyclical multi-graph. The central elements are Components. A Component can contain other components or reference them.
A Component instance is well suited to representing reference-type elements
and can contain instances of Parameter, which can represent value-type elements. Each component has an access profile. 
Collections of Component instances are stored in projects and can reference an arbitrary number of assets of any type, including geometric representations.

The geometric part of the data model contains spatial relationship semantics. The architectural space can be represented by volumes, and by the enclosing walls,
floor and ceiling or roof that define the volume’s boundary. EdgeLoops, Edges, and Vertices complete the geometric data model by allowing the representation of 1-dimensional surface boundaries,
and 1- and 0-dimensional elements, respectively. The implementation is based on the Partial Entity Structure. In order to utilize the geometry as an interface for other
design information the geometric part can be connected to the semantic-carrying part, i.e. the components.

For more details, check out the [Wiki](https://github.com/bph-tuwien/SIMULTAN/wiki)
