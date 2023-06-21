using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MathHelper : MonoBehaviour
{
    public static string FormatNumber(int number)
    {
        if (number >= 1000000000)
        {
            return (number / 1000000000f).ToString("0.#") + "T";
        }
        else if (number >= 1000000)
        {
            return (number / 1000000f).ToString("0.#") + "M";
        }
        else if (number >= 1000)
        {
            return (number / 1000f).ToString("0.#") + "K";
        }
        else
        {
            return number.ToString();
        }
    }


    public static int ParseFormattedNumber(string formattedNumber)
    {
        int multiplier = 1;
        if (formattedNumber.EndsWith("K"))
        {
            multiplier = 1000;
            formattedNumber = formattedNumber.Substring(0, formattedNumber.Length - 1);
        }
        else if (formattedNumber.EndsWith("M"))
        {
            multiplier = 1000000;
            formattedNumber = formattedNumber.Substring(0, formattedNumber.Length - 1);
        }
        else if (formattedNumber.EndsWith("T"))
        {
            multiplier = 1000000000;
            formattedNumber = formattedNumber.Substring(0, formattedNumber.Length - 1);
        }

        float result;
        if (float.TryParse(formattedNumber, out result))
        {
            result *= multiplier;
            return Mathf.RoundToInt(result);
        }
        else
        {
            throw new ArgumentException("Invalid formatted number string");
        }
    }
}
