using System;
using System.Collections.Generic;
using UnityEngine;

public class VoxelNode : OctreeNodeBase<int, VoxelTree, VoxelNode, VoxelCoordinates> {
    private readonly Dictionary<NeighbourSide, HashSet<VoxelNode>> _sideSolidChildren =
        new Dictionary<NeighbourSide, HashSet<VoxelNode>>();

    private readonly Dictionary<NeighbourSide, int> _sideSolidCount = new Dictionary<NeighbourSide, int>();

    private int _solidNodeCount;

    public VoxelNode(Bounds bounds, VoxelTree tree) : this(bounds, null, ChildIndex.Invalid, 0, tree) {}

    public VoxelNode(Bounds bounds, VoxelNode parent, ChildIndex indexInParent, int depth, VoxelTree ocTree)
        : base(bounds, parent, indexInParent, depth, ocTree) {
        _sideSolidCount[NeighbourSide.Above] = 0;
        _sideSolidCount[NeighbourSide.Below] = 0;
        _sideSolidCount[NeighbourSide.Right] = 0;
        _sideSolidCount[NeighbourSide.Left] = 0;
        _sideSolidCount[NeighbourSide.Forward] = 0;
        _sideSolidCount[NeighbourSide.Back] = 0;


        _sideSolidChildren[NeighbourSide.Above] = new HashSet<VoxelNode>();
        _sideSolidChildren[NeighbourSide.Below] = new HashSet<VoxelNode>();
        _sideSolidChildren[NeighbourSide.Right] = new HashSet<VoxelNode>();
        _sideSolidChildren[NeighbourSide.Left] = new HashSet<VoxelNode>();
        _sideSolidChildren[NeighbourSide.Forward] = new HashSet<VoxelNode>();
        _sideSolidChildren[NeighbourSide.Back] = new HashSet<VoxelNode>();
    }

    private bool SideSolid(NeighbourSide side) {
        return _sideSolidCount[side] > 0;
    }

    private SideState GetSideState(VoxelCoordinates coords, NeighbourSide side) {
        AssertNotDeleted();
        var neighbourCoords = coords.GetNeighbourCoords(side);

        //out of the boundaries
        if (neighbourCoords == null) {
            return SideState.Empty;
        }

#if USE_ALL_NODES
        OctreeNode<T> neighbourNode;

        if (_allNodes.TryGetValue(neighbourCoords.GetHashCode(), out neighbourNode)) {
            if (neighbourNode.IsLeafNode()) {
                return neighbourNode.HasItem() ? SideState.Full : SideState.Empty;
            }

            // not null and not leaf, so the neighbour node must be partial

            SideState sideState;
            if (neighbourNode.SideSolid(GetOpposite(side))) {
                // if the opposite side of current node is solid, then this is a partial node.
                sideState = SideState.Partial;
            } else {
                sideState = SideState.Empty;
            }

            return sideState;
        }

        // that child doesn't exist

        //let's check the parents
        while (neighbourCoords.Length > 0) {
            // get the next parent
            neighbourCoords = neighbourCoords.GetParentCoordinates();

            //does the next parent exist?
            if (!_allNodes.TryGetValue(neighbourCoords.GetHashCode(), out neighbourNode)) {
                continue;
            }

            if (neighbourNode.IsDeleted()) {
                continue;
            }

            // is the parent a leaf?
            if (neighbourNode.IsLeafNode()) {
                return neighbourNode.HasItem() ? SideState.Full : SideState.Empty;
            }

            // is not a leaf so cannot have an item

            break;
        }

        return SideState.Empty;
#else

        if (neighbourCoords.GetTree() == null) {
            return SideState.Empty;
        }

        var currentNode = neighbourCoords.GetTree().GetRoot();

        // follow the children until you get to the node
        foreach (var coord in neighbourCoords) {
            if (currentNode == null) {
                return SideState.Empty;
            }

            if (currentNode.IsLeafNode()) {
                return currentNode.HasItem() ? SideState.Full : SideState.Empty;
            }

            currentNode = currentNode.GetChild(coord.ToIndex());
        }

        //last currentNode is the actual node at the neighbour coordinates

        if (currentNode == null) {
            return SideState.Empty;
        }

        if (currentNode.IsLeafNode()) {
            return currentNode.HasItem() ? SideState.Full : SideState.Empty;
        }

        // not null and not leaf, so it must be partial
        // try to recursively get all nodes on this side
        SideState sideState;
        if (currentNode.SideSolid(GetOpposite(side))) {
            // if the opposite side of current node is solid, then this is a partial node.
            sideState = SideState.Partial;
        } else {
            sideState = SideState.Empty;
        }
        return sideState;
#endif
    }

    protected override void AddSolidNode(ChildIndex childIndex, bool actuallySolid) {
        NeighbourSide verticalSide, depthSide, horizontalSide;

        if (childIndex != ChildIndex.Invalid) {
            GetNeighbourSides(childIndex, out verticalSide, out horizontalSide, out depthSide);

            _sideSolidCount[verticalSide]++;
            _sideSolidCount[depthSide]++;
            _sideSolidCount[horizontalSide]++;
        } else {
            GetNeighbourSides(indexInParent, out verticalSide, out horizontalSide, out depthSide);
        }

        if (parent != null && _solidNodeCount == 0) {
            parent.AddSolidNode(indexInParent, false);
        }

        _solidNodeCount++;
        if (!actuallySolid) {
            return;
        }

        VoxelNode actualNode;
        if (childIndex != ChildIndex.Invalid) {
            actualNode = GetChild(childIndex);

            _sideSolidChildren[verticalSide].Add(actualNode);
            _sideSolidChildren[depthSide].Add(actualNode);
            _sideSolidChildren[horizontalSide].Add(actualNode);
        } else {
            actualNode = this;
        }

        var currentParent = parent;
        while (currentParent != null) {
            currentParent._sideSolidChildren[verticalSide].Add(actualNode);
            currentParent._sideSolidChildren[depthSide].Add(actualNode);
            currentParent._sideSolidChildren[horizontalSide].Add(actualNode);

            currentParent = currentParent.parent;
        }
    }

    protected override void RemoveSolidNode(ChildIndex childIndex, bool wasActuallySolid) {
        NeighbourSide verticalSide, depthSide, horizontalSide;

        if (childIndex != ChildIndex.Invalid) {
            GetNeighbourSides(childIndex, out verticalSide, out horizontalSide, out depthSide);

            _sideSolidCount[verticalSide]--;
            _sideSolidCount[depthSide]--;
            _sideSolidCount[horizontalSide]--;
        } else {
            GetNeighbourSides(indexInParent, out verticalSide, out horizontalSide, out depthSide);
        }

        _solidNodeCount--;

        if (parent != null && _solidNodeCount == 0) {
            parent.RemoveSolidNode(indexInParent, false);
        }

        if (!wasActuallySolid) {
            return;
        }

        VoxelNode actualNode;
        if (childIndex != ChildIndex.Invalid) {
            actualNode = GetChild(childIndex);

            _sideSolidChildren[verticalSide].Remove(actualNode);
            _sideSolidChildren[depthSide].Remove(actualNode);
            _sideSolidChildren[horizontalSide].Remove(actualNode);
        } else {
            actualNode = this;
        }

        var currentParent = parent;
        while (currentParent != null) {
            currentParent._sideSolidChildren[verticalSide].Remove(actualNode);
            currentParent._sideSolidChildren[depthSide].Remove(actualNode);
            currentParent._sideSolidChildren[horizontalSide].Remove(actualNode);

            currentParent = currentParent.parent;
        }
    }

    protected override void SetItemInternal(int newItem, bool cleanup, bool updateNeighbours) {
        AssertNotDeleted();

        if (!IsLeafNode()) {
            //if it's not a leaf node, we need to remove all children
            // no need to update neighbours

            RemoveAllChildren();
        }

        if (!hasItem) {
            // still let the neighbours know if necessary
            //            voxelTree.NodeRemoved(this, false);

            hasItem = true;

            AddSolidNode(ChildIndex.Invalid, true);

            item = newItem;
            ocTree.NodeAdded(this, false);
        } else if (ocTree.ItemsBelongInSameMesh(item, newItem)) {
            // has item
            // item not changed or belongs in same mesh as the other one
            item = newItem;
        } else {
            // remove from the previous item's mesh
            // no need to update neighbours now, will be done below
            ocTree.NodeRemoved(this, false);
            item = newItem;
            //add to the next item's mesh!
            ocTree.NodeAdded(this, false);
        }

        if (cleanup && parent != null && parent.childCount == 8) {
            // check if all other siblings have the same item.
            // if they do, then we can just set the parent's item instead
            for (var i = 0; i < 8; i++) {
                if (i == (int) indexInParent) {
                    continue;
                }
                var sibling = parent.GetChild((ChildIndex) i);

                if (!Equals(sibling.GetItem(), newItem)) {
                    // not all siblings have the same item :(
                    if (updateNeighbours) {
                        ocTree.UpdateNeighbours(this);
                    }

                    return;
                }
            }

            // no need to update parent's neighbours since they will be facing full sides anyway
            parent.SetItemInternal(newItem, true, false);
        } else {
            // end of the line, can update neighbours if necessary
            // it's either not cleanup,
            // or the parent doesn't exist (reached top),
            // or the parent doesn't have all eight children
            if (updateNeighbours) {
                ocTree.UpdateNeighbours(this);
            }
        }
    }

    public IEnumerable<VoxelNode> GetAllSolidNeighbours(NeighbourSide side) {
        var neighbourCoords = nodeCoordinates.GetNeighbourCoords(side);

        //out of the map!
        if (neighbourCoords == null) {
            return null;
        }

#if USE_ALL_NODES
        OctreeNode<T> neighbourNode;

        if (_allNodes.TryGetValue(neighbourCoords.GetHashCode(), out neighbourNode)) {
            if (neighbourNode.IsSolid()) {
                return new HashSet<OctreeNode<T>> {neighbourNode};
            }

            return neighbourNode._sideSolidChildren[GetOpposite(side)];
        }

        // that child doesn't exist

        //let's check the parents
        while (neighbourCoords.Length > 0) {
            // get the next parent
            neighbourCoords = neighbourCoords.GetParentCoordinates();

            //does the next parent exist?
            if (!_allNodes.TryGetValue(neighbourCoords.GetHashCode(), out neighbourNode)) {
                continue;
            }

            // is the parent a leaf?
            if (neighbourNode.IsSolid()) {
                return new HashSet<OctreeNode<T>> {neighbourNode};
            }

            // is not a leaf so cannot have an item

            break;
        }

        return null;
#else

        if (neighbourCoords.GetTree() == null) {
            return null;
        }

        var currentNeighbourNode = neighbourCoords.GetTree().GetRoot();

        foreach (var coord in neighbourCoords) {
            if (currentNeighbourNode == null || currentNeighbourNode.IsDeleted()) {
                return null;
            }

            if (currentNeighbourNode.IsSolid()) {
                return new HashSet<VoxelNode> {currentNeighbourNode};
            }

            currentNeighbourNode = currentNeighbourNode.GetChild(coord.ToIndex());
        }

        //        last currentNode is the actual node at the neighbour coordinates
        if (currentNeighbourNode == null || currentNeighbourNode.IsDeleted()) {
            return null;
        }

        if (currentNeighbourNode.IsSolid()) {
            return new HashSet<VoxelNode> {currentNeighbourNode};
        }

        return currentNeighbourNode._sideSolidChildren[GetOpposite(side)];
#endif
    }


    public HashSet<OctreeRenderFace> CreateFaces(int meshIndex) {
        AssertNotDeleted();

        var faces = new HashSet<OctreeRenderFace>();

        foreach (var side in AllSides) {
            CreateFacesForSideInternal(side, meshIndex, faces);
        }

        return faces;
    }

    private void CreateFacesForSideInternal(NeighbourSide side, int meshIndex, ICollection<OctreeRenderFace> faces) {
        CreateFacesForSideInternal(faces, side, bounds, nodeCoordinates, meshIndex);
    }


    private static OctreeChildCoordinates[] GetChildCoordsOfSide(NeighbourSide side) {
        OctreeChildCoordinates[] childCoords;

        switch (side) {
            case NeighbourSide.Above:
                childCoords = AboveCoords;
                break;
            case NeighbourSide.Below:
                childCoords = BelowCoords;
                break;
            case NeighbourSide.Right:
                childCoords = RightCoords;
                break;
            case NeighbourSide.Left:
                childCoords = LeftCoords;
                break;
            case NeighbourSide.Back:
                childCoords = BackCoords;
                break;
            case NeighbourSide.Forward:
                childCoords = ForwardCoords;
                break;
            default:
                throw new ArgumentOutOfRangeException("side", side, null);
        }
        return childCoords;
    }


    private void CreateFacesForSideInternal(ICollection<OctreeRenderFace> faces, NeighbourSide side,
        Bounds currentBounds,
        VoxelCoordinates coords, int meshIndex, bool parentPartial = false) {
        AssertNotDeleted();
        var sidestate = GetSideState(coords, side);

        switch (sidestate) {
            case SideState.Empty:
                //            case SideState.Partial:

                AddFaceToList(faces, side, currentBounds, meshIndex);
                break;
            case SideState.Partial:
                if (parentPartial) {
                    var childCoords = GetChildCoordsOfSide(side);

                    // ReSharper disable once ForCanBeConvertedToForeach
                    for (var i = 0; i < childCoords.Length; i++) {
                        var childCoord = childCoords[i];
                        var childBounds = GetChildBoundsInternal(currentBounds, childCoord.ToIndex());
                        var childAbsCoords = new VoxelCoordinates(ocTree, coords,
                            childCoord);

//                        var _coords = new Coordinates();

                        CreateFacesForSideInternal(faces, side, childBounds, childAbsCoords, meshIndex);
                    }
                } else {
                    AddFaceToList(faces, side, currentBounds, meshIndex);
                }
                break;
            case SideState.Full:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static void AddFaceToList(ICollection<OctreeRenderFace> faces, NeighbourSide side, Bounds bounds,
        int meshIndex) {
        var face = new OctreeRenderFace(meshIndex);

        var min = bounds.min;
        var max = bounds.max;

        Vector3 n;

        switch (side) {
            case NeighbourSide.Above:
                face.vertices[0] = new Vector3(min.x, max.y, min.z);
                face.vertices[1] = new Vector3(min.x, max.y, max.z);
                face.vertices[2] = max;
                face.vertices[3] = new Vector3(max.x, max.y, min.z);

                n = Vector3.up;

                face.uvs[0] = new Vector2(min.x, min.z);
                face.uvs[1] = new Vector2(min.x, max.z);
                face.uvs[2] = new Vector2(max.x, max.z);
                face.uvs[3] = new Vector2(max.x, min.z);
                break;
            case NeighbourSide.Below:
                face.vertices[0] = new Vector3(min.x, min.y, max.z);
                face.vertices[1] = min;
                face.vertices[2] = new Vector3(max.x, min.y, min.z);
                face.vertices[3] = new Vector3(max.x, min.y, max.z);

                n = Vector3.down;

                face.uvs[0] = new Vector2(min.x, max.z);
                face.uvs[1] = new Vector2(min.x, min.z);
                face.uvs[2] = new Vector2(max.x, min.z);
                face.uvs[3] = new Vector2(max.x, max.z);
                break;
            case NeighbourSide.Left:
                face.vertices[0] = new Vector3(min.x, min.y, max.z);
                face.vertices[1] = new Vector3(min.x, max.y, max.z);
                face.vertices[2] = new Vector3(min.x, max.y, min.z);
                face.vertices[3] = min;

                n = Vector3.left;

                face.uvs[0] = new Vector2(max.z, min.y);
                face.uvs[1] = new Vector2(max.z, max.y);
                face.uvs[2] = new Vector2(min.z, max.y);
                face.uvs[3] = new Vector2(min.z, min.y);
                break;
            case NeighbourSide.Right:
                face.vertices[0] = new Vector3(max.x, min.y, min.z);
                face.vertices[1] = new Vector3(max.x, max.y, min.z);
                face.vertices[2] = max;
                face.vertices[3] = new Vector3(max.x, min.y, max.z);


                n = Vector3.right;

                face.uvs[0] = new Vector2(min.z, min.y);
                face.uvs[1] = new Vector2(min.z, max.y);
                face.uvs[2] = new Vector2(max.z, max.y);
                face.uvs[3] = new Vector2(max.z, min.y);
                break;
            case NeighbourSide.Forward:
                face.vertices[0] = new Vector3(max.x, min.y, max.z);
                face.vertices[1] = max;
                face.vertices[2] = new Vector3(min.x, max.y, max.z);
                face.vertices[3] = new Vector3(min.x, min.y, max.z);

                n = Vector3.forward;

                face.uvs[0] = new Vector2(max.x, min.y);
                face.uvs[1] = new Vector2(max.x, max.y);
                face.uvs[2] = new Vector2(min.x, max.y);
                face.uvs[3] = new Vector2(min.x, min.y);
                break;
            case NeighbourSide.Back:
                face.vertices[0] = min;
                face.vertices[1] = new Vector3(min.x, max.y, min.z);
                face.vertices[2] = new Vector3(max.x, max.y, min.z);
                face.vertices[3] = new Vector3(max.x, min.y, min.z);

                n = Vector3.back;

                face.uvs[0] = new Vector2(min.x, min.y);
                face.uvs[1] = new Vector2(min.x, max.y);
                face.uvs[2] = new Vector2(max.x, max.y);
                face.uvs[3] = new Vector2(max.x, min.y);
                break;
            default:
                throw new ArgumentOutOfRangeException("side", side, null);
        }

        face.normal = n;

        faces.Add(face);
    }

    private enum SideState {
        Empty,
        Partial,
        Full
    }
}