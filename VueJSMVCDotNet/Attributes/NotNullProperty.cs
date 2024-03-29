﻿namespace VueJSMVCDotNet.Attributes
{
    /// <summary>
    /// Used to specify a property of a model cannot be set to null (this is used where the property type cannot be identified as nullable or not properly like a string)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property,AllowMultiple =false,Inherited =false)]
    public class NotNullProperty : Attribute
    {
    }
}
