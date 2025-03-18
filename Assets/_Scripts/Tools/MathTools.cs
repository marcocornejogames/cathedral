using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Timeline;

public static class MathTools
{
    public static Vector2 LocationWithinDistance2D(float minDistance, float maxDistance, Vector2 originalTrasform)
    {
        float absMinDis = Mathf.Abs(minDistance);
        float absMaxDis = Mathf.Abs(maxDistance);
        float locationX = originalTrasform.x + PlusOrMinus(Random.Range(absMinDis, absMaxDis));
        float locationY = originalTrasform.y + PlusOrMinus(Random.Range(absMinDis, absMaxDis));
        Vector2 newLocation  = new Vector2 (locationX, locationY);
        return newLocation;
    }

    public static float PlusOrMinus(float multiplier)
    {
        float x = Random.Range(-1,1);
        float r = Mathf.Sign(x) * multiplier;
        return r;
    }

    public static Vector2 Flatten2D(UnityEngine.Vector3 v3)
    {
        Vector2 v2 = new Vector2(v3.x, v3.y);
        return v2;
    }

    public static Vector2 GetVector2Average(List<Vector2> listV2)
    {
        Vector2 average = new Vector2(0,0);
        for(int i = 0; i < listV2.Count; i++)
        {
            average += listV2[i];
        }

        average = average/listV2.Count;
        return average;
    }

    public static float Remap (this float from, float fromMin, float fromMax, float toMin,  float toMax) //From RazaTech, unity.com
    {
        var fromAbs  =  from - fromMin;
        var fromMaxAbs = fromMax - fromMin;       
       
        var normal = fromAbs / fromMaxAbs;

        var toMaxAbs = toMax - toMin;
        var toAbs = toMaxAbs * normal;

        var to = toAbs + toMin;
       
        return to;
    }

    public static Quaternion GetRotationAngle(Vector3 direction, float rotationModifier, Transform axis)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - rotationModifier;
        return Quaternion.AngleAxis(angle, axis.forward);
    }

    public static void ShuffleTextures(Texture2D[] array)
    {
        // Knuth shuffle algorithm :: courtesy of Wikipedia :)
        for (int t = 0; t < array.Length; t++ )
        {
            Texture2D tmp = array[t];
            int r = Random.Range(t, array.Length);
            array[t] = array[r];
            array[r] = tmp;
        }
    
    }

}

