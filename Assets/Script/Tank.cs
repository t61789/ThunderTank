using System;
using Tool;
using UnityEngine;
using Vector2 = System.Numerics.Vector2;

namespace ThunderTank
{
    public class Tank:BaseEntity
    {
        
    }

    [Serializable]
    public class AccSwing
    {
        public float SwingAngle=20;
        public float SwingRecoverFactor = 1;
        public float SwingDamp=1;

        private float _SwingValue;
        private float _PreForwardVelocity;
        private Vector2 _PrePos;

        public float CurSwingAngle => _SwingValue * SwingAngle;

        public void Init(Vector2 curPos)
        {
            _PrePos = curPos;
        }

        // todo 亟待测试
        public void FixedUpdate(Vector2 curPos,Vector2 forwardDir)
        {
            var forwardVelocity= Vector2.Dot(curPos - _PrePos, forwardDir);
            forwardVelocity -= _PreForwardVelocity;
            var changeForce = forwardVelocity * forwardVelocity;
            var recoverForce = SwingRecoverFactor * _SwingValue * _SwingValue * -Math.Sign(_SwingValue);
            var dampForce = forwardVelocity * forwardVelocity * SwingDamp * -Math.Sign(recoverForce);
            var sumForce = changeForce+recoverForce + dampForce;
            _SwingValue += sumForce;

            _PreForwardVelocity = forwardVelocity;
            _PrePos = curPos;
        }
    }

    [Serializable]
    public class TankTurret
    {
        public float TurnSpeed;
        public float PitchAngle;
        public float MaxAimDistance;

        private Transform _TurretTrans;
        private Transform _BarrelTrans;

        public Vector3 AimPos { private set; get; }

        public void Init(Transform turretTrans)
        {
            _TurretTrans = turretTrans;
            _BarrelTrans = _TurretTrans.Find("Barrel");
        }

        // todo 亟待测试
        public void FixedUpdate(Vector3 aimPos)
        {
            aimPos = _TurretTrans.worldToLocalMatrix * aimPos;
            var targetRot = Quaternion.LookRotation(aimPos.ProjectToxz());
            _TurretTrans.localRotation = Quaternion.RotateTowards(_TurretTrans.localRotation,targetRot,TurnSpeed);

            var targetPitch = Vector3.Angle(aimPos.ProjectToxz(), aimPos) * Mathf.Sign(aimPos.y);
            targetPitch = targetPitch.Clamp(-PitchAngle,PitchAngle);
            _BarrelTrans.localEulerAngles = new Vector3(targetPitch,0,0);

            UpdateAimPos();
        }

        private void UpdateAimPos()
        {
            var ray = new Ray(_BarrelTrans.position,_BarrelTrans.forward);
            var hits = Physics.RaycastAll(ray);
            if (hits.Length != 0)
                AimPos = hits[0].point;
            else
                AimPos = _BarrelTrans.position + _BarrelTrans.forward * MaxAimDistance;
        }
    }
}