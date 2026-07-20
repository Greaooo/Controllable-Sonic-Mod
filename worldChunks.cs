using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Mod;
using CharApi;


namespace WorldChunks
{ 
    public class SnapPoint : MonoBehaviour
    {
        void Awake()
        {
            CircleCollider2D coll = gameObject.AddComponent<CircleCollider2D>();
            coll.radius = 0.15f;
            coll.isTrigger = true;
        }
    }

    public class WorldChunk : MonoBehaviour
    {
        public bool isPlaced { get; private set; } = false;

        public Vector2 restingPlace { get; private set; }
        public Vector2 roundedMousePos { get; private set; }

        public EdgeCollider2D collider { get; private set; }

        GameObject[] _snapObjs;

        bool snap;

        Vector2 snapPos;

        public Sprite snapDebug;

        public void Update()
        {
            if (isPlaced)
            {
                transform.position = Vector2.Lerp(transform.position, roundedMousePos, Time.deltaTime * 15);
                CheckForStatus();
                return;
            }
            else
            {
                Move();
            }
        }

        public void Move()
        {
            roundedMousePos = new Vector2(Mathf.Round(Global.main.MousePosition.x), Mathf.Round(Global.main.MousePosition.y));

            if (Input.GetMouseButtonDown(0))
            {
                Place();
            }
            if (Input.GetMouseButtonDown(1))
            {
                Cancel();
            }

            Vector3 mousePos = Global.main.MousePosition;
            mousePos.Set(mousePos.x, mousePos.y, 0);

            if (!snap)
            {
                transform.position = mousePos;
                snapPos = mousePos;
                CheckForSnapPoints();
            }
            else
            {
                transform.position = Vector2.Lerp(transform.position, snapPos, Time.deltaTime * 15);
                if (Vector2.Distance(mousePos, transform.position) > 2)
                {
                    snap = false;   
                }
            }
        }

        void CheckForSnapPoints()
        {
            foreach (GameObject point in _snapObjs)
            {
                Collider2D[] colls = Physics2D.OverlapCircleAll(point.transform.position, 1);

                ModAPI.Notify(colls);

                for (int i = 0; i < colls.Length; i++)
                {
                    if (colls[i] == point) { continue; }

                    if (colls[i].GetComponent<SnapPoint>())
                    {
                        snapPos = colls[i].transform.position - point.transform.localPosition *
                            transform.localScale.x;

                        snap = true;
                    }
                }
            }
        }

        void CheckForStatus()
        {
            if (Global.main.ShowLimbStatus)
            {
                ShowSnapPoints(true);
            }
            else
            {
                ShowSnapPoints(false);
            }
        }

        void ShowSnapPoints(bool show)
        {
            switch (show)
            {
                case true:
                    foreach (GameObject point in _snapObjs)
                    {
                        point.GetComponent<SpriteRenderer>().sprite = snapDebug;
                    }
                    break;
                case false:
                    foreach (GameObject point in _snapObjs)
                    {
                        point.GetComponent<SpriteRenderer>().sprite = null;
                    }
                    break;
            }
        }

        public void Place()
        {
            isPlaced = true;

            restingPlace = snapPos;
        }

        public void Cancel()
        {
            GameObject.Destroy(gameObject);
        }

        public void SetSnapPoints(GameObject[] newPoints)
        {
            _snapObjs = newPoints;
            foreach (GameObject point in _snapObjs)
            {
                point.AddComponent<SnapPoint>();
                SpriteRenderer re = point.AddComponent<SpriteRenderer>();
                re.sprite = null;
            }
        }

        public void SetEdgeCollider(EdgeCollider2D edgeCollider)
        {
            collider = edgeCollider;
        }

        public void DisableCollision()
        {
            collider.enabled = false;
        }

        public void EnableCollision()
        {
            collider.enabled = true;
        }
    }
}