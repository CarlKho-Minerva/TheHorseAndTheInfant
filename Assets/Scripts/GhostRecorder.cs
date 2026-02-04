using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct GhostFrame {
    public Vector3 position;
    public Quaternion rotation;
    public bool isAttacking;
    public int attackType;
}

public class GhostRecorder : MonoBehaviour
{
    public bool isRecording = true;
    public List<GhostFrame> recordedFrames = new List<GhostFrame>();

    private SimpleCombo comboScript;

    void Start() {
        comboScript = GetComponent<SimpleCombo>();
    }

    void FixedUpdate() {
        if (isRecording) {
            GhostFrame frame = new GhostFrame();
            frame.position = transform.position;
            frame.rotation = transform.rotation;

            if (comboScript != null) {
                frame.isAttacking = comboScript.justAttacked;
                frame.attackType = comboScript.currentAttackAnim;
            }

            recordedFrames.Add(frame);
        }
    }
}