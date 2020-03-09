using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Habrador_Computational_Geometry
{
    //Help methods related to interpolation
    public static class InterpolationHelpMethods
    {
        //
        // Split a curve into sections with some resolution
        //

        //Steps is the number of sections we are going to split the curve in
        //So the number of interpolated values are steps + 1
        //tEnd is where we want to stop measuring if we dont want to split the entire curve, so tEnd is maximum of 1
        public static List<MyVector3> SplitCurve_CubicBezier(MyVector3 pA, MyVector3 pB, MyVector3 hA, MyVector3 hB, int steps, float tEnd)
        {
            //Store the interpolated values so we later can display them
            List<MyVector3> interpolatedPositions = new List<MyVector3>();

            //Loop between 0 and tStop in steps. If tStop is 1 we loop through the entire curve
            //1 step is minimum, so if steps is 5 then the line will be cut in 5 sections
            float stepSize = tEnd / (float)steps;

            float t = 0f;

            //+1 becuase wa also have to include the first point
            for (int i = 0; i < steps + 1; i++)
            {
                //Debug.Log(t);

                MyVector3 interpolatedValue = _Interpolation.BezierCubic(pA, pB, hA, hB, t);

                interpolatedPositions.Add(interpolatedValue);

                t += stepSize;
            }

            return interpolatedPositions;
        }
    
    

        //
        // Calculate length of curve
        //

        //Get the length of the curve with a naive method where we divide the
        //curve into straight lines and then measure the length of each line
        //tEnd is 1 if we want to get the length of the entire curve
        public static float GetLengthNaive_CubicBezier(MyVector3 pA, MyVector3 pB, MyVector3 hA, MyVector3 hB, int steps, float tEnd)
        {
            //Split the ruve into positions with some steps resolution
            List<MyVector3> CurvePoints = SplitCurve_CubicBezier(pA, pB, hA, hB, steps, tEnd);


            //Calculate the length by measuring the length of each step
            float length = 0f;

            for (int i = 1; i < CurvePoints.Count; i++)
            {
                float thisStepLength = MyVector3.Distance(CurvePoints[i - 1], CurvePoints[i]);

                length += thisStepLength;
            }

            return length;
        }



        //Get the length by using Simpson's Rule (not related to the television show)
        //https://www.youtube.com/watch?v=J_a4PXI_nLY
        //The basic idea is that we cut the curve into sections and each section is approximated by a polynom 
        public static float GetLengthSimpsonsRule_CubicBezier(MyVector3 posA, MyVector3 posB, MyVector3 handleA, MyVector3 handleB, float tStart, float tEnd)
        {
            //Divide the curve into sections
            
            //How many sections?
            int n = 10;

            //The width of each section
            float delta = (tEnd - tStart) / (float)n;


            //The main loop to calculate the length

            //Everything multiplied by 1
            float derivativeStart = MyVector3.Magnitude(Derivative_CubicBezier(posA, posB, handleA, handleB, tStart));
            float derivativeEnd = MyVector3.Magnitude(Derivative_CubicBezier(posA, posB, handleA, handleB, tEnd));

            float endPoints = derivativeStart + derivativeEnd;


            //Everything multiplied by 4
            float x4 = 0f;
            for (int i = 1; i < n; i += 2)
            {
                float t = tStart + delta * i;

                x4 += MyVector3.Magnitude(Derivative_CubicBezier(posA, posB, handleA, handleB, t));
            }


            //Everything multiplied by 2
            float x2 = 0f;
            for (int i = 2; i < n; i += 2)
            {
                float t = tStart + delta * i;

                x2 += MyVector3.Magnitude(Derivative_CubicBezier(posA, posB, handleA, handleB, t));
            }


            //The final length
            float length = (delta / 3f) * (endPoints + 4f * x4 + 2f * x2);


            return length;
        }



        //
        // Calculate the derivative at a point on a curve
        //

        //Alternative 1. Estimate the derivative at point t
        //https://www.youtube.com/watch?v=jvYZNp5myXg
        //https://www.alanzucconi.com/2017/04/10/robotic-arms/
        public static MyVector3 EstimateDerivative_CubicBezier(MyVector3 posA, MyVector3 posB, MyVector3 handleA, MyVector3 handleB, float t)
        {
            //We can estimate the derivative by taking a step in each direction of the point we are interested in
            //Should be around this number
            float derivativeStepSize = 0.0001f;

            MyVector3 valueMinus = _Interpolation.BezierCubic(posA, posB, handleA, handleB, t - derivativeStepSize);
            MyVector3 valuePlus = _Interpolation.BezierCubic(posA, posB, handleA, handleB, t + derivativeStepSize);

            //Have to multiply by two because we are taking a step in each direction
            MyVector3 derivativeVector = (valuePlus - valueMinus) * (1f / (derivativeStepSize * 2f));

            return derivativeVector;
        }



        //Alternative 2. Actual derivative at point t
        public static MyVector3 Derivative_CubicBezier(MyVector3 posA, MyVector3 posB, MyVector3 handleA, MyVector3 handleB, float t)
        {
            MyVector3 A = posA;
            MyVector3 B = handleA;
            MyVector3 C = handleB;
            MyVector3 D = posB;

            //Layer 1
            //(1-t)A + tB = A - At + Bt
            //(1-t)B + tC = B - Bt + Ct
            //(1-t)C + tD = C - Ct + Dt

            //Layer 2
            //(1-t)(A - At + Bt) + t(B - Bt + Ct) = A - At + Bt - At + At^2 - Bt^2 + Bt - Bt^2 + Ct^2 = A - 2At + 2Bt + At^2 - 2Bt^2 + Ct^2 
            //(1-t)(B - Bt + Ct) + t(C - Ct + Dt) = B - Bt + Ct - Bt + Bt^2 - Ct^2 + Ct - Ct^2 + Dt^2 = B - 2Bt + 2Ct + Bt^2 - 2Ct^2 + Dt^2

            //Layer 3
            //(1-t)(A - 2At + 2Bt + At^2 - 2Bt^2 + Ct^2) + t(B - 2Bt + 2Ct + Bt^2 - 2Ct^2 + Dt^2)
            //A - 2At + 2Bt + At^2 - 2Bt^2 + Ct^2 - At + 2At^2 - 2Bt^2 - At^3 + 2Bt^3 - Ct^3 + Bt - 2Bt^2 + 2Ct^2 + Bt^3 - 2Ct^3 + Dt^3
            //A - 3At + 3Bt + 3At^2 - 6Bt^2 + 3Ct^2 - At^3 + 3Bt^3 - 3Ct^3 + Dt^3
            //A - 3t(A - B) + t^2(3A - 6B + 3C) + t^3(-A + 3B - 3C + D)
            //A - 3t(A - B) + t^2(3(A - 2B + C)) + t^3(-(A - 3(B - C) - D)

            //The derivative: -3(A - B) + 2t(3(A - 2B + C)) + 3t^2(-(A - 3(B - C) - D)
            //-3(A - B) + t(6(A - 2B + C)) + t^2(-3(A - 3(B - C) - D)

            MyVector3 derivativeVector = t * t * (-3f * (A - 3f * (B - C) - D));

            derivativeVector += t * (6f * (A - 2f * B + C));

            derivativeVector += -3f * (A - B);

            return derivativeVector;
        }


        public static MyVector3 Derivative_QuadraticBezier(MyVector3 posA, MyVector3 posB, MyVector3 handle, float t)
        {
            MyVector3 A = posA;
            MyVector3 B = handle;
            MyVector3 C = posB;

            //Layer 1
            //(1-t)A + tB = A - At + Bt
            //(1-t)B + tC = B - Bt + Ct

            //Layer 2
            //(1-t)(A - At + Bt) + t(B - Bt + Ct)
            //A - At + Bt - At + At^2 - Bt^2 + Bt - Bt^2 + Ct^2
            //A - 2At + 2Bt + At^2 - 2Bt^2 + Ct^2 
            //A - t(2(A - B)) + t^2(A - 2B + C)

            //Derivative: -(2(A - B)) + t(2(A - 2B + C))

            MyVector3 derivativeVector = t * (2f * (A - 2f * B + C));

            derivativeVector += -2f * (A - B);


            return derivativeVector;
        }



        //
        // Divide the curve into constant steps
        //

        //The problem with the t-value and Bezier curves is that the t-value is NOT percentage along the curve
        //So sometimes we need to divide the curve into equal steps, for example if we generate a mesh along the curve

        //Alternative 1
        //Use Newton–Raphsons method to find which t value we need to travel distance d on the curve
        //d is measured from the start of the curve in [m] so is not the same as paramete t which is [0, 1]
        //https://en.wikipedia.org/wiki/Newton%27s_method
        //https://www.youtube.com/watch?v=-mad4YPAn2U
        public static MyVector3 FindPointToTravelDistance_CubicBezier_Iterative(MyVector3 pA, MyVector3 pB, MyVector3 hA, MyVector3 hB, float d, float totalLength)
        {
            //Need a start value to make the method start
            //Should obviously be between 0 and 1
            //We can say that a good starting point is the percentage of distance traveled
            //If this start value is not working you can use the Bisection Method to find a start value
            //https://en.wikipedia.org/wiki/Bisection_method
            float t = d / totalLength;
            
            //Need an error so we know when to stop the iteration
            float error = 0.001f;

            //We also need to avoid infinite loops
            int iterations = 0;

            while (true)
            {
                //The derivative and the length can be calculated in different ways
            
                //The derivative vector at point t
                MyVector3 derivativeVec = EstimateDerivative_CubicBezier(pA, pB, hA, hB, t);
                //MyVector3 derivativeVec = DerivativeCubicBezier(pA, pB, hA, hB, t);

                //The length of the curve to point t from the start
                //float lengthTo_t = GetLengthNaive_CubicBezier(pA, pB, hA, hB, steps: 20, tEnd: t);
                float lengthTo_t = GetLengthSimpsonsRule_CubicBezier(pA, pB, hA, hB, tStart: 0f, tEnd: t);


                //Calculate a better t with Newton's method: x_n+1 = x_n + (f(x_n) / f'(x_n))
                //Our f(x) = lengthTo_t - d = 0. We want them to be equal because we want to find the t value 
                //that generates a distance(t) which is the same as the d we want. So when f(x) is close to zero we are happy
                //When we take the derivative of f(x), d disappears which is why we are not subtracting it in the bottom
                float tNext = t - ((lengthTo_t - d) / MyVector3.Magnitude(derivativeVec));


                //Have we reached the desired accuracy?
                float diff = tNext - t;

                t = tNext;

                //Have we found a t to get a distance which matches the distance we want?  
                if (diff < error && diff > -error)
                {
                    //Debug.Log("d: " + d + " t: " + t + " Distance: " + GetLengthSimpsonsCubicBezier(posA, posB, handleA, handleB, tStart: 0f, tEnd: tNext));

                    break;
                }


                //Safety so we don't get stuck in an infinite loop
                iterations += 1;

                if (iterations > 1000)
                {
                    Debug.Log("Couldnt find a t value within the iteration limit");

                    break;
                }
            }


            //Now we can calculate the point on the curve by using the new t
            MyVector3 pointOnCurve = _Interpolation.BezierCubic(pA, pB, hA, hB, t);

            return pointOnCurve;
        }



        //Alternative 2
        //Create a lookup-table with distances along the curve, then interpolate these distances
        //This is faster but less accurate than using the iterative Newton–Raphsons method
        //We can use the lookup table in the newton-raphson method


        //
        // Get actual t-value
        //

        //Parameter t is not always percentage along the curve
        //Sometimes we need to calculate the actual percentage if t had been percentage along the curve
        //From https://www.youtube.com/watch?v=o9RK6O2kOKo
        public static float FindActualPercentageAlongCurve_CubicBezier(MyVector3 pA, MyVector3 pB, MyVector3 hA, MyVector3 hB, float tBad)
        {
            //Step 1. Find positions on the curve by using the bad t-value
            int steps = 20;

            List<MyVector3> positionsOnCurve = SplitCurve_CubicBezier(pA, pB, hA, hB, steps, tEnd: 1f);


            //Step 2. Calculate the cumulative distances along the curve for each position along the curve 
            //we just calculated
            List<float> distances = new List<float>();

            float totalDistance = 0f;

            distances.Add(totalDistance);

            for (int i = 1; i < positionsOnCurve.Count; i++)
            {
                totalDistance += MyVector3.Distance(positionsOnCurve[i], positionsOnCurve[i - 1]);

                distances.Add(totalDistance);
            }

            //Debug.Log(distances[distances.Count - 1]);

            //TODO Step 1 and 2 can be pre-calculated


            //Step 3. Find the positions in the distances list tBad is closest to and interpolate between them
            if (distances == null || distances.Count == 0)
            {
                Debug.Log("Cant interpolate to split bezier into equal steps");
                
                return 0f;
            }

            //If we have just one value, just return it
            if (distances.Count == 1)
            {
                return distances[0] / totalDistance;
            }


            //Convert the t-value to an array position
            //t-value can be seen as percentage, so we get percentage along the list of values
            //If we have 5 values in the list, we have 4 buckets, so if t is 0.65, we get 0.65*4 = 2.6
            float arrayPosBetween = tBad * (float)(distances.Count - 1);
            
            //Round up and down to get the actual array positions
            int arrayPosL = Mathf.FloorToInt(arrayPosBetween); //2 if we follow the example above 
            int arrayPosR = Mathf.FloorToInt(arrayPosBetween + 1f); //2.6 + 1 = 3.6 -> 3 

            //If we reached too high return the last value
            if (arrayPosR >= distances.Count)
            {
                return distances[positionsOnCurve.Count - 1] / totalDistance;
            }
            //Too low
            else if (arrayPosR < 0f)
            {
                return distances[0] / totalDistance;
            }

            //Interpolate by lerping
            float percentage = arrayPosBetween - arrayPosL; //2.6 - 2 = 0.6 if we follow the example above 

            //(1f - t) * a + t * b; so if percentage is 0.6 we should get more of the one to the right
            float interpolatedDistance = _Interpolation.Lerp(distances[arrayPosL], distances[arrayPosR], percentage);

            //This is the actual t-value that we should have used to get to this distance
            //So if tBad is 0.8 it doesnt mean that we have travelled 80 percent along the curve
            //If tBad is 0.8 and tActual is 0.7, it means that we have actually travelled 70 percent along the curve
            float tActual = interpolatedDistance / totalDistance;

            //Debug.Log("t-bad: " + tBad + " t-actual: " + tActual);

            

            return tActual;
        }
    }
}
