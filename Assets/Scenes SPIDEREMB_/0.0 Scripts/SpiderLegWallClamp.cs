using System;
using UnityEngine;

[DefaultExecutionOrder(9000)] // spät genug, damit Anim/Steppers vorher laufen; bei Bedarf in Project Settings noch weiter nach hinten setzen
public class SpiderLegWallClamp : MonoBehaviour
{
    [Serializable]
    public class Leg
    {
        [Tooltip("Obere Referenz (z. B. Hüft-/Schulter-Gelenk der Bein-Kette).")]
        public Transform hip;

        [Tooltip("Das IK-Target/Endeffektor, das dein IK benutzt (wird von diesem Skript direkt versetzt!).")]
        public Transform footTarget;

        [Header("Kollision")]
        [Tooltip("Radius der Prüfkugel (ungefähr Zehengröße).")]
        public float castRadius = 0.03f;

        [Tooltip("Sicherheitsabstand aus der Wand heraus.")]
        public float pushOut = 0.01f;

        [Tooltip("Wie weit wir von Hüfte zum gewünschten Fuß schauen (Auto: Distanz Hüfte→Foot). 0 = Auto.")]
        public float maxCastDistance = 0f;

        [Header("Glättung")]
        [Tooltip("0 = sofort, >0 = weich nachführen (Sekunden).")]
        public float smoothTime = 0.02f;

        [Tooltip("Y-Höhe des Fußes beibehalten (gut für senkrechte Wände).")]
        public bool preserveY = true;

        // intern
        [NonSerialized] public Vector3 _vel;
    }

    [Tooltip("Wand-/Boden-Layer, mit denen die Beine blocken sollen.")]
    public LayerMask solidMask = ~0;

    [Tooltip("Liste deiner Beine (Hip + IK-Target).")]
    public Leg[] legs;

    [Tooltip("Kleinstes Delta, bevor wir überhaupt reagieren (gegen Mikrojitter).")]
    public float ignoreDeltaBelow = 0.00001f;

    void LateUpdate()
    {
        if (legs == null) return;

        for (int i = 0; i < legs.Length; i++)
        {
            var L = legs[i];
            if (!L.hip || !L.footTarget) continue;

            Vector3 desired = L.footTarget.position;         // Position, die dein Stepper/Anim gesetzt hat
            Vector3 from    = L.hip.position;
            Vector3 toDir   = desired - from;
            float   dist    = toDir.magnitude;

            if (dist < ignoreDeltaBelow) continue;

            // SphereCast von Hüfte in Richtung Fuß
            float castDistance = (L.maxCastDistance > 0f) ? L.maxCastDistance : dist;
            Vector3 dir = toDir / Mathf.Max(1e-6f, dist);

            // ganz leicht "zurücktreten", damit der Cast nicht im Startpunkt steckt
            Vector3 castOrigin = from - dir * 0.005f;

            if (Physics.SphereCast(castOrigin, L.castRadius, dir, out var hit, castDistance + 0.01f,
                                   solidMask, QueryTriggerInteraction.Ignore))
            {
                // Wand liegt zwischen Hüfte und Fuß → an Oberfläche festklemmen
                Vector3 target = hit.point + hit.normal * L.pushOut;

                if (L.preserveY)
                    target.y = desired.y;   // keine abrupten Höhen-Sprünge

                // weich oder sofort setzen
                if (L.smoothTime > 0f)
                {
                    Vector3 pos = L.footTarget.position;
                    pos = Vector3.SmoothDamp(pos, target, ref L._vel, L.smoothTime);
                    L.footTarget.position = pos;
                }
                else
                {
                    L.footTarget.position = target;
                }
            }
            else
            {
                // keine Wand dazwischen → optional sanft zurück zur gewünschten Position
                if (L.smoothTime > 0f)
                {
                    Vector3 pos = L.footTarget.position;
                    pos = Vector3.SmoothDamp(pos, desired, ref L._vel, L.smoothTime);
                    L.footTarget.position = pos;
                }
                else
                {
                    // nichts tun: dein Stepper hat schon desired gesetzt
                }
            }
        }
    }
}
