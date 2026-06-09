using UnityEngine;

public class WireManagement : MonoBehaviour
{
    public GameObject wirePrefab;
    public void AddWire(Vector3 position, float intensity, DoubleWireMagneticField dwField, GameObject doubleWireObject) {
        dwField.AddWire(position.x, position.y, intensity);
        GameObject newWire = Instantiate(wirePrefab, transform);
        newWire.transform.parent = doubleWireObject.transform;
        newWire.transform.position = position;
        newWire.tag = "Wire";

        LineRenderer lr = newWire.GetComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, new Vector3(position.x, position.y, -5f));
        lr.SetPosition(1, new Vector3(position.x, position.y, 5f));
        lr.startWidth = lr.endWidth = 0.1f; 
        newWire.SetActive(true);

        VRWireDrag wireDragScript = newWire.GetComponent<VRWireDrag>();
        wireDragScript.wireIndex = dwField.GetWireCount() - 1;
        wireDragScript.isDoubleWire = true;
    }

    public void RemoveWire(int index, DoubleWireMagneticField dwField, GameObject doubleWireObject) {
        dwField.RemoveWire(index);
        GameObject wireToRemove = doubleWireObject.transform.GetChild(index).gameObject;
        wireToRemove.transform.SetParent(null);
        Destroy(wireToRemove);

        for (int i = 0; i < doubleWireObject.transform.childCount; i++) {
            GameObject wire = doubleWireObject.transform.GetChild(i).gameObject;
            VRWireDrag wireDragScript = wire.GetComponent<VRWireDrag>();
            wireDragScript.wireIndex = i;
        }
    }
}
