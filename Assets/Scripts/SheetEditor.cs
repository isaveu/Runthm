﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class SheetEditor : MonoBehaviour
{
    public SheetEditorController sheetController;
    public GridGenerator gridGenerator;
    public Music music;

    public GameObject cursurObj;
    public GameObject note;

    GameObject seletedObject; // 배치 단계에서 선택된 오브젝트
    int currentSelectedLine;
    int currentBarNumber;
    float interpolValue;

    public List<int> noteLine1;
    public List<int> noteLine2;
    public List<int> noteLine3;
    public List<int> noteLine4;

    public GameObject realNote;

    public bool isPlay = false;

    public float Speed { get; set; } = 4f;
    float divSpeed;

    // 스냅
    Vector3 snapPos;
    float snapAmount;

    // Start is called before the first frame update
    void Start()
    {
        divSpeed = 1 / Speed;
        interpolValue = music.BeatPerSec32rd * 0.5f;
        // 32비트를 반으로 나눈값을 빼주는 이유 : 노트 시간값이 (float -> int)오가는 과정에서 미미한 값 오차 발생으로 인해, 이를 보정하기 위한 현재까지 건드리지 않을 64비트의 시간값을 빼줌
    }

    // Update is called once per frame
    void Update()
    {
        if(sheetController.mRay.transform != null)
            DisposePreObject();
    }

    public void SelectObject(string name)
    {
        if (name == "Note")
            sheetController.cursurObj = cursurObj;
    }

    // 노트 사전 배치(스냅)
    void DisposePreObject()
    {
        if (sheetController.mRay.transform.gameObject.layer == 8)
        {
            GameObject gridObject;
            Grid grid;
            gridObject = sheetController.mRay.transform.gameObject; // 레이받은(클릭된) 그리드의 게임오브젝트를 가져온다.
            grid = gridObject.GetComponent<Grid>(); // 그리고 해당 오브젝트의 스크릡트를 가져옴
            currentBarNumber = grid.barNumber;
            //Debug.Log("마디 번호 : " + grid.barNumber);

            // 아래는 레이의 위치를 강제로 제한, 현재는 하단 커버의 위치를 그리드보다 앞으로 나오게함으로써 그리드 레이를 읽을 수 없는 상황
            //Vector3 interpolMousePos = new Vector3(sheetController.mRay.point.x, Mathf.Clamp(sheetController.mRay.point.y, 0f, sheetController.mRay.point.y), sheetController.mRay.point.z);
            Vector3 hitToGrid; // 레이좌표(마우스월드좌표)에서 레이받은 그리드의 좌표를 빼준다. 그래야 항상 올바른 상대적 좌표를 가져올 수 있다.
            hitToGrid = sheetController.mRay.point - gridObject.transform.position;

            //Debug.Log("월드 마우스 : " + sheetController.mRay.point);
            //Debug.Log("그리드 포지 : " + sheetController.mRay.transform.position);
            //Debug.Log("히트투그리드 : " + hitToGrid);
            //Debug.Log("노트 포지 : " + (hitToGrid.y + (music.BarPerSec * grid.barNumber * Speed)));

            ProcessSnapPos(hitToGrid, gridObject);

            if (isPlay)
            {
                sheetController.cursurObj.SetActive(false);
            }
            else
            {
                sheetController.cursurObj.SetActive(true);
                sheetController.CursurEffectPos = snapPos;

                if (sheetController.mClickNum == 0)
                    DisposeObject(gridObject);
                else if (sheetController.mClickNum == 1)
                    UnDisposeObject(gridObject);
            }
        }
        else
            sheetController.cursurObj.SetActive(false);
    }

    // 오브젝트가 존재하는지 확인
    bool CheckObject(GameObject gridObject)
    {
        GameObject noteContainer = gridObject.transform.GetChild(32).gameObject;
        bool isOverlap = false;

        if (noteContainer.transform.childCount == 0)
        {
            //Debug.Log("0인데 추가");
            return isOverlap;
        }
        else
        {
            for (int i = 0; i < noteContainer.transform.childCount; i++)
            {
                isOverlap = false;
                Vector3 note = noteContainer.transform.GetChild(i).transform.position;
                
                if (Mathf.Approximately(note.x, snapPos.x) && (note.y >= snapPos.y - interpolValue && note.y <= snapPos.y + interpolValue))
                {
                    Debug.Log("이미 노트가 있습니다.");
                    seletedObject = noteContainer.transform.GetChild(i).transform.gameObject;
                    isOverlap = true;
                    break;
                }
            }
            return isOverlap;
        }
    }  
    // 오브젝트 배치
    void DisposeObject(GameObject gridObject)
    {
        GameObject noteContainer = gridObject.transform.GetChild(32).gameObject;

        if (!CheckObject(gridObject))
        {
            GameObject obj = Instantiate(note, snapPos, Quaternion.identity, noteContainer.transform);
            float pos = obj.transform.localPosition.y + (currentBarNumber * music.BarPerSec * Speed);
            SaveObject(currentSelectedLine, pos);
        }

    }
    // 오브젝트 언배치
    void UnDisposeObject(GameObject gridObject)
    {
        if (CheckObject(gridObject))
        {
            float pos = seletedObject.transform.localPosition.y + (currentBarNumber * music.BarPerSec * Speed);
            DeleteObject(currentSelectedLine, pos);

            Destroy(seletedObject);
        }
    }

    void ProcessSnapPos(Vector3 hitToGrid, GameObject gridObject)
    {
        // 현재 스냅양에 따라 스냅될 위치를 계산한다. (x값)
        float snapPosX = 0f;
        if (sheetController.mRay.point.x > -5f && sheetController.mRay.point.x < -2.5f)
        {
            snapPosX = -3.75f;
            currentSelectedLine = 1;
        }
        else if (sheetController.mRay.point.x > -2.5f && sheetController.mRay.point.x < 0f)
        {
            snapPosX = -1.25f;
            currentSelectedLine = 2;
        }
        else if (sheetController.mRay.point.x > 0f && sheetController.mRay.point.x < 2.5f)
        {
            snapPosX = 1.25f;
            currentSelectedLine = 3;
        }
        else if (sheetController.mRay.point.x > 2.5f && sheetController.mRay.point.x < 5f)
        {
            snapPosX = 3.75f;
            currentSelectedLine = 4;
        }

        // 현재 스냅양에 따라 스냅될 위치를 계산한다. (y값)
        float snapAmount = gridGenerator.ScrollSnapAmount * music.BeatPerSec32rd * Speed;
        float halfSnapAmount = snapAmount / 2;

        float snapPosY = hitToGrid.y;
        for (int i = 0; i < 32 / gridGenerator.ScrollSnapAmount; i++)
        {
            if (snapPosY >= (snapAmount * i) - halfSnapAmount && snapPosY <= (snapAmount * i) + halfSnapAmount)
            {
                //Debug.Log("최소 : " + ((snapAmount * i) - halfSnapAmount) + " 최대 : " + ((snapAmount * i) + halfSnapAmount));
                //Debug.Log("걸린 곳 : " + i);
                snapPos = new Vector3(snapPosX, gridObject.transform.position.y + i * snapAmount, -0.1f);

                break;
            }
        }
    }

    void SaveObject(int line, float pos)
    {
        float time = pos * 1000f * divSpeed;

        if (line == 1)
            noteLine1.Add((int)time);
        else if (line == 2)
            noteLine2.Add((int)time);
        else if (line == 3)
            noteLine3.Add((int)time);
        else if (line == 4)
            noteLine4.Add((int)time);
    }

    void DeleteObject(int line, float pos)
    {
        Debug.Log("오브젝트가 있었는데요. 없었습니다.");

        float time = pos * 1000f * divSpeed;

        if (line == 1 && noteLine1.Contains((int)time))
            noteLine1.Remove((int)time);
        else if (line == 2 && noteLine2.Contains((int)time))
            noteLine2.Remove((int)time);
        else if (line == 3 && noteLine3.Contains((int)time))
            noteLine3.Remove((int)time);
        else if (line == 4 && noteLine4.Contains((int)time))
            noteLine4.Remove((int)time);
    }

    void GenNote()
    {
        float convertedTime;
        float standardTime;
        standardTime = music.BarPerSec - interpolValue;
        GameObject gridObj;
        GameObject noteContainer;

        int index = 0;

        for (int j = 0; j < noteLine1.Count; j++)
        {
            convertedTime = noteLine1[j] * 0.001f;

            if (convertedTime >= standardTime)
            {
                index++;
                standardTime *= index + 1;
            }

            gridObj = gridGenerator.grids[index];
            noteContainer = gridObj.transform.GetChild(32).gameObject;

            GameObject obj = Instantiate(note, new Vector3(-3.75f, music.Offset + convertedTime * Speed, 0f), Quaternion.identity, noteContainer.transform);
            obj.SetActive(true);
        }
        for (int j = 0; j < noteLine2.Count; j++)
        {
            convertedTime = noteLine2[j] * 0.001f;

            if (convertedTime >= standardTime)
            {
                index++;
                standardTime *= index + 1;
            }

            gridObj = gridGenerator.grids[index];
            noteContainer = gridObj.transform.GetChild(32).gameObject;

            GameObject obj = Instantiate(note, new Vector3(-1.25f, music.Offset + convertedTime * Speed, 0f), Quaternion.identity, noteContainer.transform);
            obj.SetActive(true);
        }
        for (int j = 0; j < noteLine3.Count; j++)
        {
            convertedTime = noteLine3[j] * 0.001f;

            if (convertedTime >= standardTime)
            {
                index++;
                standardTime *= index + 1;
            }

            gridObj = gridGenerator.grids[index];
            noteContainer = gridObj.transform.GetChild(32).gameObject;

            GameObject obj = Instantiate(note, new Vector3(1.25f, music.Offset + convertedTime * Speed, 0f), Quaternion.identity, noteContainer.transform);
            obj.SetActive(true);
        }
        for (int j = 0; j < noteLine4.Count; j++)
        {
            convertedTime = noteLine4[j] * 0.001f;

            if (convertedTime >= standardTime)
            {
                index++;
                standardTime *= index + 1;
            }

            gridObj = gridGenerator.grids[index];
            noteContainer = gridObj.transform.GetChild(32).gameObject;

            GameObject obj = Instantiate(note, new Vector3(3.75f, music.Offset + convertedTime * Speed, 0f), Quaternion.identity, noteContainer.transform);
            obj.SetActive(true);
        }
    }

    public void Save()
    {
        string data = "";

        noteLine1.Sort();
        noteLine2.Sort();
        noteLine3.Sort();
        noteLine4.Sort();

        using (StreamWriter streamWriter = new StreamWriter(new FileStream(Application.dataPath + "/Resources/" + "Milky Way.txt", FileMode.Create, FileAccess.Write), System.Text.Encoding.Unicode))
        {
            foreach (int note in noteLine1)
            {
                data = note.ToString() + ",1";
                streamWriter.WriteLine(data);
            }
            foreach (int note in noteLine2)
            {
                data = note.ToString() + ",2";
                streamWriter.WriteLine(data);
            }
            foreach (int note in noteLine3)
            {
                data = note.ToString() + ",3";
                streamWriter.WriteLine(data);
            }
            foreach (int note in noteLine4)
            {
                data = note.ToString() + ",4";
                streamWriter.WriteLine(data);
            }
        }
    }

    public void Load()
    {
        string data = "";

        using (StreamReader streamReader = new StreamReader(Application.dataPath + "/Resources/" + "Milky Way.txt"))
        {
            while ((data = streamReader.ReadLine()) != null)
            {
                Parse(data);
            }
        }

        GenNote();
    }


    void Parse(string rawData) // 추후 스크립트 분리
    {
        string[] splitedData = new string[2];
        int time;
        int lineNumber;

        splitedData = rawData.Split(',');

        time = int.Parse(splitedData[0]);
        lineNumber = int.Parse(splitedData[1]);

        if (lineNumber == 1)
            noteLine1.Add(time);
        else if (lineNumber == 2)
            noteLine2.Add(time);
        else if (lineNumber == 3)
            noteLine3.Add(time);
        else if (lineNumber == 4)
            noteLine4.Add(time);
    }
}
