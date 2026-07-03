using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Rendering;

public enum CellTypes
{
    Empty = -1,
    ZeroRunoff,
    OneRunoff,
    TwoRunoff,
    ThreeRunoff,
    FourRunoff,
    FiveRunoff,
    SixRunoff,
    ZeroRight,
    OneRight,
    TwoRight,
    ThreeRight,
    FourRight,
    FiveRight,
    SixRight,
    ZeroDown,
    OneDown,
    TwoDown,
    ThreeDown,
    FourDown,
    FiveDown,
    SixDown
}
// if positive: use % 7 to extract value of the domino
// and / 7 to extract direction (if it's 0, then its a runoff of a domino tile one left or up)
// if negative, the tile is empty.


public class GameInstance : MonoBehaviour
{
    int[][] AvailablePieces =
    {
        new int[]{0, 0},
        new int[]{0, 1},
        new int[]{0, 2},
        new int[]{0, 3},
        new int[]{0, 4},
        new int[]{0, 5},
        new int[]{0, 6},
        new int[]{1, 1},
        new int[]{1, 2},
        new int[]{1, 3},
        new int[]{1, 4},
        new int[]{1, 5},
        new int[]{1, 6},
        new int[]{2, 2},
        new int[]{2, 3},
        new int[]{2, 4},
        new int[]{2, 5},
        new int[]{2, 6},
        new int[]{3, 3},
        new int[]{3, 4},
        new int[]{3, 5},
        new int[]{3, 6},
        new int[]{4, 4},
        new int[]{4, 5},
        new int[]{4, 6},
        new int[]{5, 5},
        new int[]{5, 6},
        new int[]{6, 6}
    };
    
    const int STANDARD_LENGTH = 8;
    const int STANDARD_HEIGHT = 8;
    int PlayFieldLength = 0;
    int PlayFieldHeight = 0;
    public int[] currentDrawOrder;
    public int[] nextDrawOrder;
    public int currentPieceAsIndex;
    public Vector2Int placement;
    public Vector2Int placement2;
    public int[,] PlayField;
    public int score;
    public System.Random rng;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // If rng exists, use it; otherwise creates a new one
        rng ??= new();
        OnBoardStart();
        // has to do this before OnDeckout call, because the NextDrawOrder is still empty for now
        nextDrawOrder = (int[])Enumerable.Range(0, 28);
        OnDeckout();
    }

    void FixedUpdate()
    {
        
    }
    void OnBoardStart()
    {
        if (PlayFieldLength <= 0)
        {
            PlayFieldLength = STANDARD_LENGTH;
        }
        if (PlayFieldHeight <= 0)
        {
            PlayFieldHeight = STANDARD_HEIGHT;
        }
        PlayField = new int[PlayFieldLength, PlayFieldHeight];
        Span<int> flattenedPlayField = MemoryMarshal.CreateSpan(ref PlayField[0, 0], PlayField.Length);
        flattenedPlayField.Fill((int) CellTypes.Empty);
        OnBoardRefresh();
    }
    void OnBoardRefresh()
    {
        int[] slotsX = {PlayFieldLength / 2};
        int[] slotsY = {PlayFieldHeight / 2};
        int xl = 1;
        int yh = 1;
        if (PlayFieldLength % 2 == 0)
        {
            slotsX.Append(slotsX[0] + 1);
            xl = 2;
            if (PlayFieldHeight % 2 == 0)
            {
                slotsY.Append(slotsY[0] + 1);
                yh = 2;
            }
        } else
        {
            slotsY.Append(slotsY[0] + 1);
            yh = 2;
        }
        // Decide if the placement would be vertical or horizontal.
        bool isHorizontal = yh == 1;
        if (yh > 1 && xl > 1)
        {
            isHorizontal = rng.Next(2) == 0;
            
        }
        // Do a Perfectly Different Starting Dominos using this method
        int[] todraw = (int[])Enumerable.Range(0, 28);
        int[] alldrawn = new int[] {};
        Shuffle(todraw);
        todraw = todraw[..(yh * xl / 2)]; // ddx
        foreach(int item in todraw)
        {
            int[] drawn = AvailablePieces[item];
            if (rng.Next(2) == 0)
            {
                (drawn[1], drawn[0]) = (drawn[0], drawn[1]);
            }
            alldrawn.Concat(drawn);
        }
        for(int i = 0;i < xl; i++)
        {
            for(int j = 0;j < yh; j++)
            {

                PlayField[slotsX[i],slotsY[j]] = alldrawn[i * yh + j];
                if (isHorizontal && i == 0)
                {
                    // offset Horizontal mode
                    PlayField[slotsX[i],slotsY[j]] += 7;
                } 
                else if(j == 0)
                {
                    // offset Vertical mode
                    PlayField[slotsX[i],slotsY[j]] += 14;
                }
            }
        }
    }
    public void OnDeckout()
    {
        // Refill the current and next Roll.
        currentDrawOrder = (int[])nextDrawOrder.Clone();
        nextDrawOrder =  (int[])Enumerable.Range(0, 28);
        Shuffle(nextDrawOrder);

    }
public void Shuffle<T>(T[] a)
{
    int n = a.Length;
    
    for(;n > 1;n--)
    {
        int k = rng.Next(n + 1);
        T value = a[k];
        a[k] = a[n];
        a[n] = value;
    }
}

    public void OnPlayerTryRotateCursor(bool cw)
    {
        // kick the cursor in if the rotation makes it outside of the playfield
        Vector2Int kickDirection = new (0, 0);
        if((placement2.x < placement.x && cw) || (placement2.x > placement.x && !cw)) 
            // Left cw or Right ccw -> Up
        {
            placement2 = new (placement.x, placement.y + 1);
            if (placement2.y >= PlayFieldHeight)
            {
                kickDirection.y -= 1;
            }
        } 
        else if((placement2.y < placement.y && cw) || (placement2.y > placement.y && !cw))
        {
            // Down cw or up ccw -> Left
            placement2 = new (placement.x - 1, placement.y);
            if (placement2.x < 0)
            {
                kickDirection.x += 1;
            }
        } 
        else if(placement2.x != placement.x) // Right + cw or left ccw -> Down
        {
            placement2 = new (placement.x, placement.y - 1);
            if (placement2.y < 0)
            {
                kickDirection.y += 1;
            }
        } 
        else // Up cw or down ccw -> Right
        {
            placement2 = new (placement.x + 1, placement.y);
            if (placement2.x >= PlayFieldLength)
            {
                kickDirection.x -= 1;
            }
        }
        

        placement += kickDirection;
        placement2 += kickDirection;
    }
    void OnPlayerTryMoveCursor(Vector2Int dir) {
        dir.Clamp(-Vector2Int.Min(placement, placement2), Vector2Int.Max(placement, placement2));
        
        
        
        placement += dir;
        placement2 += dir;
        
    }

    // check if this tile is already occupied,
    // check if neighboring cells is already occupied
    // (excluding walls)
    bool PlayerCanPlaceAtCursor()
    {
        int [,] directions = {{1,0}, {0,1}, {-1, 0}, {0, -1}};
        
        if ((PlayField[placement.x, placement.y] != (int) CellTypes.Empty) || (PlayField[placement2.x, placement2.y] != (int) CellTypes.Empty))
        {
            return false;
        }
        for(int i = 0;i < 4; i++)
        {
            if(placement.x + directions[i, 0] >= 0 && placement.y + directions[i, 1] >= 0 &&
            placement.x + directions[i, 0] < PlayFieldLength && placement.y + directions[i, 1] < PlayFieldHeight)
            {
                if (PlayField[placement.x + directions[i, 0], placement.y + directions[i, 1]] != (int) CellTypes.Empty)
                {
                    return true;
                }
            }
            // same logic for secondary tile
            if(placement2.x + directions[i, 0] >= 0 && placement.y + directions[i, 1] >= 0 &&
            placement.x + directions[i, 0] < PlayFieldLength && placement.y + directions[i, 1] < PlayFieldHeight)
            {
                if (PlayField[placement2.x + directions[i, 0], placement2.y + directions[i, 1]] != (int) CellTypes.Empty)
                {
                    return true;
                }
            }
        }
        return false;
    }

    void OnPlayerTryPlace()
    {
        if (!PlayerCanPlaceAtCursor())
        {
            return;
        }
        
    }
    void TryClear()
    {
        
    }
}
