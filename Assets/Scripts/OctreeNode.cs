﻿//#define USE_ALL_NODES

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

public enum NeighbourSide {
    Above = 0,
    Below = 1,
    Right = 2,
    Left = 3,
    Back = 4,
    Forward = 5,
    Invalid = -1
}

public abstract class OctreeNode {
    public enum ChildIndex {
        Invalid = -1,

        LeftBelowBack = 0,
        RightBelowBack = 1,

        LeftAboveBack = 2,
        RightAboveBack = 3,

        LeftBelowForward = 4,
        RightBelowForward = 5,

        LeftAboveForward = 6,
        RightAboveForward = 7
    }


    private static readonly Vector3 RightAboveBack = new Vector3(1, 1, -1);
    private static readonly Vector3 LeftAboveBack = new Vector3(-1, 1, -1);

    private static readonly Vector3 RightAboveForward = new Vector3(1, 1, 1);
    private static readonly Vector3 LeftAboveForward = new Vector3(-1, 1, 1);

    private static readonly Vector3 RightBelowBack = new Vector3(1, -1, -1);
    private static readonly Vector3 LeftBelowBack = new Vector3(-1, -1, -1);

    private static readonly Vector3 RightBelowForward = new Vector3(1, -1, 1);
    private static readonly Vector3 LeftBelowForward = new Vector3(-1, -1, 1);

    public static readonly NeighbourSide[] AllSides = {
        NeighbourSide.Left,
        NeighbourSide.Right,
        NeighbourSide.Below,
        NeighbourSide.Above,
        NeighbourSide.Back,
        NeighbourSide.Forward
    };

    protected static readonly OctreeChildCoordinates[] LeftCoords = {
        new OctreeChildCoordinates(0, 0, 0),
        new OctreeChildCoordinates(0, 1, 0),
        new OctreeChildCoordinates(0, 0, 1),
        new OctreeChildCoordinates(0, 1, 1)
    };

    protected static readonly OctreeChildCoordinates[] RightCoords = {
        new OctreeChildCoordinates(1, 0, 0),
        new OctreeChildCoordinates(1, 1, 0),
        new OctreeChildCoordinates(1, 0, 1),
        new OctreeChildCoordinates(1, 1, 1)
    };

    protected static readonly OctreeChildCoordinates[] AboveCoords = {
        new OctreeChildCoordinates(0, 1, 0),
        new OctreeChildCoordinates(0, 1, 1),
        new OctreeChildCoordinates(1, 1, 0),
        new OctreeChildCoordinates(1, 1, 1)
    };

    protected static readonly OctreeChildCoordinates[] BelowCoords = {
        new OctreeChildCoordinates(0, 0, 0),
        new OctreeChildCoordinates(0, 0, 1),
        new OctreeChildCoordinates(1, 0, 0),
        new OctreeChildCoordinates(1, 0, 1)
    };

    protected static readonly OctreeChildCoordinates[] BackCoords = {
        new OctreeChildCoordinates(0, 0, 0),
        new OctreeChildCoordinates(0, 1, 0),
        new OctreeChildCoordinates(1, 0, 0),
        new OctreeChildCoordinates(1, 1, 0)
    };

    protected static readonly OctreeChildCoordinates[] ForwardCoords = {
        new OctreeChildCoordinates(0, 0, 1),
        new OctreeChildCoordinates(0, 1, 1),
        new OctreeChildCoordinates(1, 0, 1),
        new OctreeChildCoordinates(1, 1, 1)
    };

    protected static Vector3 GetChildDirection(ChildIndex childIndex) {
        Vector3 childDirection;

        switch (childIndex) {
            case ChildIndex.RightAboveBack:
                childDirection = RightAboveBack;
                break;
            case ChildIndex.LeftAboveBack:
                childDirection = LeftAboveBack;
                break;
            case ChildIndex.RightAboveForward:
                childDirection = RightAboveForward;
                break;
            case ChildIndex.LeftAboveForward:
                childDirection = LeftAboveForward;
                break;
            case ChildIndex.RightBelowBack:
                childDirection = RightBelowBack;
                break;
            case ChildIndex.LeftBelowBack:
                childDirection = LeftBelowBack;
                break;
            case ChildIndex.RightBelowForward:
                childDirection = RightBelowForward;
                break;
            case ChildIndex.LeftBelowForward:
                childDirection = LeftBelowForward;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        return childDirection;
    }

    public static NeighbourSide GetOpposite(NeighbourSide side) {
        switch (side) {
            case NeighbourSide.Above:
                return NeighbourSide.Below;
            case NeighbourSide.Below:
                return NeighbourSide.Above;
            case NeighbourSide.Right:
                return NeighbourSide.Left;
            case NeighbourSide.Left:
                return NeighbourSide.Right;
            case NeighbourSide.Back:
                return NeighbourSide.Forward;
            case NeighbourSide.Forward:
                return NeighbourSide.Back;
            default:
                throw new ArgumentOutOfRangeException("side", side, null);
        }
    }


    protected static void GetNeighbourSides(ChildIndex childIndex,
        out NeighbourSide verticalSide,
        out NeighbourSide horizontalSide,
        out NeighbourSide depthSide) {
        switch (childIndex) {
            case ChildIndex.Invalid:
                // self
                verticalSide = NeighbourSide.Invalid;
                horizontalSide = NeighbourSide.Invalid;
                depthSide = NeighbourSide.Invalid;
                break;
            case ChildIndex.LeftBelowBack:
                verticalSide = NeighbourSide.Below;
                depthSide = NeighbourSide.Back;
                horizontalSide = NeighbourSide.Left;
                break;
            case ChildIndex.RightBelowBack:
                verticalSide = NeighbourSide.Below;
                depthSide = NeighbourSide.Back;
                horizontalSide = NeighbourSide.Right;
                break;
            case ChildIndex.LeftAboveBack:
                verticalSide = NeighbourSide.Above;
                depthSide = NeighbourSide.Back;
                horizontalSide = NeighbourSide.Left;
                break;
            case ChildIndex.RightAboveBack:
                verticalSide = NeighbourSide.Above;
                depthSide = NeighbourSide.Back;
                horizontalSide = NeighbourSide.Right;
                break;
            case ChildIndex.LeftBelowForward:
                verticalSide = NeighbourSide.Below;
                depthSide = NeighbourSide.Forward;
                horizontalSide = NeighbourSide.Left;
                break;
            case ChildIndex.RightBelowForward:
                verticalSide = NeighbourSide.Below;
                depthSide = NeighbourSide.Forward;
                horizontalSide = NeighbourSide.Right;
                break;
            case ChildIndex.LeftAboveForward:
                verticalSide = NeighbourSide.Above;
                depthSide = NeighbourSide.Forward;
                horizontalSide = NeighbourSide.Left;
                break;
            case ChildIndex.RightAboveForward:
                verticalSide = NeighbourSide.Above;
                depthSide = NeighbourSide.Forward;
                horizontalSide = NeighbourSide.Right;
                break;
            default:
                throw new ArgumentOutOfRangeException("childIndex", childIndex, null);
        }
    }
}

public abstract partial class OctreeNodeBase<TItem, TTree, TNode> : OctreeNode
    where TTree : OctreeBase<TItem, TNode, TTree>
    where TNode : OctreeNodeBase<TItem, TTree, TNode> {
    protected readonly TTree ocTree;
    protected readonly Bounds bounds;
    protected readonly TNode parent;

    protected OctreeNodeBase(Bounds bounds, TTree tree) : this(bounds, null, ChildIndex.Invalid, 0, tree) {}

    protected OctreeNodeBase(Bounds bounds, TNode parent, ChildIndex indexInParent, int depth, TTree ocTree) {
        deleted = false;
        this.bounds = bounds;
        this.parent = parent;
        if (parent == null) {
            nodeCoordinates = new OctreeNodeBase.Coordinates();

#if USE_ALL_NODES
    // ReSharper disable once UseObjectOrCollectionInitializer
            _allNodes = new Dictionary<int, OctreeNode<T>>();
            _allNodes[nodeCoordinates.GetHashCode()] = this;
#endif
        }
        else {
#if USE_ALL_NODES
            _allNodes = _root._allNodes;
#endif

            nodeCoordinates = new OctreeNodeBase.Coordinates(parent.nodeCoordinates, OctreeChildCoordinates.FromIndex(indexInParent));
        }

        this.indexInParent = indexInParent;
        item = default(TItem);
        this.depth = depth;
        this.ocTree = ocTree;
    }

    protected bool deleted;
#if USE_ALL_NODES
    private readonly Dictionary<int, TSelf> _allNodes;
#endif
    //    private readonly OctreeChildCoordinates[] _coords;
    protected readonly int depth;
    protected readonly ChildIndex indexInParent;
    protected readonly OctreeNodeBase.Coordinates nodeCoordinates;

    protected TNode GetRoot() {
        return ocTree.GetRoot();
    }

//    protected readonly Octree<T> _tree;
    protected int childCount;
    protected TNode[] children;
    protected bool hasItem;

    protected TItem item;


    //    private List<OctreeNode<T>> _actuallySolidChildren = new List<OctreeNode<T>>(); 

    protected virtual void AddSolidNode(ChildIndex childIndex, bool actuallySolid) {}

    protected virtual void RemoveSolidNode(ChildIndex childIndex, bool wasActuallySolid) {}

    protected void AssertNotDeleted() {
        Assert.IsFalse(deleted, "Node Deleted");
    }

    public TItem GetItem() {
        AssertNotDeleted();
        return item;
    }

    public TNode GetChildAtCoords(IEnumerable<OctreeChildCoordinates> coords) {
        AssertNotDeleted();
        return GetChildAtCoords(coords.Select(coord => coord.ToIndex()));
    }

    public TNode GetChildAtCoords(IEnumerable<ChildIndex> coords) {
        AssertNotDeleted();
        var current = (TNode) this;

        foreach (var coord in coords) {
            current = current.GetChild(coord);
            if (current == null) {
                break;
            }
        }

        return current;
    }


    [Pure]
    public bool IsLeafNode() {
        AssertNotDeleted();
        return childCount == 0;
    }

    [Pure]
    public int GetChildCount() {
        AssertNotDeleted();
        return childCount;
    }

    public TNode GetChild(ChildIndex index) {
        AssertNotDeleted();

        var intIndex = (int) index;

        if (intIndex < 0 || intIndex > 7) {
            throw new ArgumentOutOfRangeException("index", "Invalid index specified for GetChild.");
        }

        if (children == null) {
            return null;
        }

        return children[(int) index];
    }

    private TNode SetChild(ChildIndex index, TNode child) {
        AssertNotDeleted();
#if USE_ALL_NODES
        child.AssertNotDeleted();
        _allNodes.Add(child.nodeCoordinates.GetHashCode(), child);
#endif

        children[(int) index] = child;
        return child;
    }

    public IEnumerable<TNode> GetChildren() {
        AssertNotDeleted();
        if (children == null) {
            yield break;
        }

        foreach (var child in children.Where(child => child != null)) {
            yield return child;
        }
    }

    protected static Bounds GetChildBoundsInternal(Bounds originalBounds, ChildIndex childIndex) {
        var childDirection = GetChildDirection(childIndex);

        var childSize = originalBounds.extents;

        var childBounds = new Bounds(originalBounds.center + Vector3.Scale(childSize, childDirection * 0.5f), childSize);

        return childBounds;
    }

    private Bounds GetChildBounds(ChildIndex childIndex) {
        AssertNotDeleted();

        return GetChildBoundsInternal(bounds, childIndex);
    }

    //recursive, can be phantom bounds!
    [Pure]
    public Bounds GetChildBounds(OctreeNodeBase.Coordinates coordinates) {
        AssertNotDeleted();

        var result = GetBounds();
        var rootSize = result.size;

        var count = 1;

        var up = 0;
        var right = 0;
        var forward = 0;

        foreach (var coordinate in coordinates) {
            count++;

            up = up * 2 + coordinate.y * 2 - 1;
            right = right * 2 + coordinate.x * 2 - 1;
            forward = forward * 2 + coordinate.z * 2 - 1;
            //            if (child != null) {
            //                // current child (or self) isn't null, try to get the real child
            //                child = child.GetChild(coordinate.ToIndex());
            //
            //                if (child != null) {
            //                    // the child is there, no need to calculate!
            //                    result = child.GetBounds();
            //                    continue;
            //                }
            //            }
            //            // no child there... get phantom bounds
            //            result = GetChildBoundsInternal(result, coordinate.ToIndex());
        }

        var upVector = Vector3.up;
        var rightVector = Vector3.right;
        var forwardVector = Vector3.forward;

        var center = (upVector * (rootSize.y * up) +
                      rightVector * (rootSize.x * right) +
                      forwardVector * (rootSize.z * forward)) / Mathf.Pow(2, count);

        var size = rootSize / Mathf.Pow(2, count - 1);

        return new Bounds(center, size);
    }


    public TNode AddChild(ChildIndex index) {
        if (index == ChildIndex.Invalid) {
            throw new ArgumentOutOfRangeException("index", "Cannot create a child at an invalid index.");
        }

        AssertNotDeleted();

        if (children == null) {
            children = new TNode[8];
        }

        if (GetChild(index) != null) {
            throw new ArgumentException("There is already a child at this index", "index");
        }

        childCount++;
        return SetChild(index, ocTree.ConstructNode(GetChildBounds(index), (TNode) this, index, depth + 1));
    }

    public void RemoveChild(ChildIndex index, bool cleanup = false) {
        RemoveChildInternal(index, cleanup, true);
    }

    private void RemoveChildInternal(ChildIndex index, bool cleanup, bool updateNeighbours) {
        AssertNotDeleted();
        if (children == null) {
            throw new ArgumentException("The child at that index is already removed!", "index");
        }

        var indexInt = (int) index;

        if (children[indexInt] != null) {
            childCount--;
        } else {
            throw new ArgumentException("The child at that index is already removed!", "index");
        }

        children[indexInt].SetDeleted(updateNeighbours);
        children[indexInt] = null;

        if (childCount != 0) {
            return;
        }

        children = null;

        if (cleanup && parent != null) {
            // no need to update parent's neighbours!
            parent.RemoveChildInternal(indexInParent, true, false);
        }
    }

    private void SetDeleted(bool updateNeighbours) {
        //calling toarray here to force enumeration to flag deleted
        foreach (var octreeNode in BreadthFirst().ToArray()) {
#if USE_ALL_NODES
            _allNodes.Remove(octreeNode.nodeCoordinates.GetHashCode());
#endif
            ocTree.NodeRemoved(octreeNode, updateNeighbours);

            if (octreeNode.hasItem) {
                octreeNode.RemoveItemInternal(updateNeighbours);
            }
            octreeNode.deleted = true;
        }
    }

    public bool IsDeleted() {
        return deleted;
    }

    public void AddBounds(Bounds newBounds, TItem newItem, int remainingDepth) {
        AssertNotDeleted();
        if (remainingDepth <= 0) {
            SetItem(newItem);
            return;
        }

        for (var i = 0; i < 8; i++) {
            var octreeNodeChildIndex = (ChildIndex) i;
            var childBounds = GetChildBounds(octreeNodeChildIndex);

            if (childBounds.Intersects(newBounds)) {
                var child = GetChild(octreeNodeChildIndex);
                if (child == null) {
                    if (HasItem()) {
                        SubDivide();
                        child = GetChild(octreeNodeChildIndex);
                    } else {
                        child = AddChild(octreeNodeChildIndex);
                    }
                }

                if (newBounds.Contains(childBounds.min) && newBounds.Contains(childBounds.max)) {
                    //child intersects and is completely contained by it

                    child.SetItem(newItem);
                } else {
                    //child intersects but is not completely contained by it

                    child.AddBounds(newBounds, newItem, remainingDepth - 1);
                }
            }
        }
    }

    public void SetItem(TItem newItem, bool cleanup = false) {
        SetItemInternal(newItem, cleanup, true);
    }

    protected virtual void SetItemInternal(TItem newItem, bool cleanup, bool updateNeighbours) {}

    protected void RemoveAllChildren() {
        for (var i = 0; i < 8; i++) {
            if (GetChild((ChildIndex) i) != null) {
                // no need to update neighbours
                RemoveChildInternal((ChildIndex) i, false, false);
            }
        }
    }

    public bool HasItem() {
        return hasItem;
    }

    public void RemoveItem() {
        RemoveItemInternal(true);
    }

    private void RemoveItemInternal(bool updateNeighbours) {
        if (hasItem) {
            ocTree.NodeRemoved((TNode) this, updateNeighbours);

            RemoveSolidNode(ChildIndex.Invalid, true);

            hasItem = false;
        }

        item = default(TItem);
    }

    public Bounds GetBounds() {
        AssertNotDeleted();
        return bounds;
    }

    public OctreeNodeBase.Coordinates GetCoords() {
        AssertNotDeleted();
        return nodeCoordinates;
    }

    // https://en.wikipedia.org/wiki/Breadth-first_search#Pseudocode

    /*
1     procedure BFS(G,v) is
2      let Q be a queue
3      Q.enqueue(v)
4      label v as discovered
5      while Q is not empty
6         v ← Q.dequeue()
7         process(v)
8         for all edges from v to w in G.adjacentEdges(v) do
9             if w is not labeled as discovered
10                 Q.enqueue(w)
11                label w as discovered
    */

    public IEnumerable<TNode> BreadthFirst() {
        AssertNotDeleted();
        var queue = new Queue<TNode>();
        queue.Enqueue((TNode) this);

        var discovered = new HashSet<TNode> {GetRoot()};

        while (queue.Count > 0) {
            var node = queue.Dequeue();
            yield return node;

            foreach (var child in node.GetChildren().Where(child => !discovered.Contains(child))) {
                queue.Enqueue(child);
                discovered.Add(child);
            }
        }
    }

    // https://en.wikipedia.org/wiki/Depth-first_search#Pseudocode
    /*
    1  procedure DFS-iterative(G,v):
    2      let S be a stack
    3      S.push(v)
    4      while S is not empty
    5            v = S.pop() 
    6            if v is not labeled as discovered:
    7                label v as discovered
    8                for all edges from v to w in G.adjacentEdges(v) do
    9                    S.push(w)
    */

    public IEnumerable<TNode> DepthFirst() {
        AssertNotDeleted();
        var stack = new Stack<TNode>();
        stack.Push((TNode) this);

        while (stack.Count > 0) {
            var node = stack.Pop();
            yield return node;

            foreach (var child in node.GetChildren()) {
                stack.Push(child);
            }
        }
    }

    public ChildIndex GetIndexInParent() {
        return indexInParent;
    }

    public void SubDivide(bool fillChildren = true) {
        AssertNotDeleted();

        if (!IsLeafNode()) {
            // if it's not a leaf node then it's already divided
            return;
        }

        var oldItem = item;

        RemoveItemInternal(false);

        for (var i = 0; i < 8; i++) {
            var newChild = AddChild((ChildIndex) i);

            if (fillChildren) {
                newChild.SetItemInternal(oldItem, false, false);
            }
        }
    }

    public bool IsSolid() {
        return IsLeafNode() && HasItem();
    }

    public TNode AddRecursive(OctreeNodeBase.Coordinates coordinates) {
        var node = (TNode) this;

        foreach (var coordinate in coordinates) {
            var index = coordinate.ToIndex();

            var child = node.GetChild(index);
            if (child != null) {
                node = child;
            } else {
                if (node.HasItem()) {
                    node.SubDivide();
                    node = node.GetChild(index);
                } else {
                    node = node.AddChild(index);
                }
            }
        }

        return node;
    }


    public TNode GetParent() {
        return parent;
    }

    public void RemoveRecursive(OctreeNodeBase.Coordinates coordinates, bool cleanup = false) {
        if (coordinates.Length == 0) {
            return;
        }

        var node = this;

        var subdivided = false;

        var nodeItem = default(TItem);

        foreach (var coordinate in coordinates) {
            var index = coordinate.ToIndex();

            if (!subdivided) {
                var child = node.GetChild(index);
                if (child != null) {
                    // has a child at that node, so go deeper
                    node = child;
                    continue;
                }

                // no child at that node
                if (!node.HasItem()) {
                    //it doesn't have an item! but the child is null!? then it has a child somewhere else, so nothing to remove!
                    return;
                }

                nodeItem = node.GetItem();

                node.SubDivide(false);

                // set the items for all other children manually
                for (var i = 0; i < 8; ++i) {
                    var childIndex = (ChildIndex) i;

                    if (childIndex != index) {
                        // do not update neighbours as they will mostly be full
                        node.GetChild(childIndex).SetItemInternal(nodeItem, false, false);
                    }
                }

                node = node.GetChild(index);

                subdivided = true;
            } else {
                // subdivision of parent happened, from now on we need to add children ourselves
                // because the current node will not have any children or item defined

                // create all children and set the items for all but the next child manually
                for (var i = 0; i < 8; ++i) {
                    var childIndex = (ChildIndex) i;
                    var newChild = node.AddChild(childIndex);

                    if (childIndex != index) {
                        newChild.SetItemInternal(nodeItem, false, false);
                    }
                }

                node = node.GetChild(index);
            }
        }

        // remove final child now
        // this will result in an update for the neighbours
        node.GetParent().RemoveChildInternal(node.GetIndexInParent(), cleanup, true);
    }


    public TTree GetTree() {
        return ocTree;
    }

//    public OctreeNode(Bounds bounds, Octree<T> _tree) : base(bounds, tree) { }
//    public OctreeNode(Bounds bounds, OctreeNode<T> parent, ChildIndex indexInParent, int depth, Octree<T> _tree) : base(bounds, parent, indexInParent, depth, tree) { }
}