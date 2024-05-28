/* 
/// Copyright (c) 2015 Sirawat Pitaksarit, Exceed7 Experiments LP 
/// http://www.exceed7.com/introloop
*/

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class ImportantIntAttribute : PropertyAttribute {
	
	public readonly string unit;
    public readonly int stepSize;
	
	public ImportantIntAttribute ()
	{
        this.stepSize = 1;
    }   

    public ImportantIntAttribute (int stepSize)
	{
        this.stepSize = stepSize;
    }                                   

    public ImportantIntAttribute (string unit)
	{
		this.unit = unit;
    }                                   
	
    public ImportantIntAttribute (string unit, int stepSize)
	{
		this.unit = unit;
        this.stepSize = stepSize;
    }                                   
}                                      
                                        
