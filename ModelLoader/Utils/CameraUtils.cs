/*
 * Copyright (C) 2024 David Shelley <davidsmithshelley@gmail.com>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License 
 * along with this program. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Windows.Media.Media3D; //Point3D stuff

namespace ModelLoader
{
    internal static class CameraUtils
    {
        internal static void MoveCameraLeft(PerspectiveCamera camera, double distance)
        {
            // Calculate the right direction
            Vector3D rightDirection = Vector3D.CrossProduct(camera.UpDirection, camera.LookDirection);

            // Normalize the right direction to ensure it's a unit vector
            rightDirection.Normalize();

            // Calculate the new position by moving left
            const double sensitivity = 0.005;
            double finalDistance = distance * sensitivity;

            Point3D newPosition = camera.Position - (rightDirection * finalDistance);

            // Update the camera's position
            camera.Position = newPosition;
        }

        internal static void MoveCameraUp(PerspectiveCamera camera, double distance)
        {
            Vector3D lookNormalized = camera.LookDirection;
            lookNormalized.Normalize();
            Vector3D cameraRightDirection = Vector3D.CrossProduct(lookNormalized, camera.UpDirection);
            cameraRightDirection.Normalize();
            Vector3D upNormalized = Vector3D.CrossProduct(cameraRightDirection, lookNormalized);
            upNormalized.Normalize();

            const double sensitivity = 0.005;
            double finalDistance = distance * sensitivity;

            // Calculate the new position by moving up
            Point3D newPosition = camera.Position - (upNormalized * finalDistance);
            // Update the camera's position
            camera.Position = newPosition;
        }

        internal static void MoveCameraForward(Model3DGroup sceneGroup, PerspectiveCamera camera, int delta)
        {
            if (camera == null) { return; }


            Point3D origin = new Point3D(0, 0, 0);
            Vector3D cameraToTarget = camera.Position - origin;
            double distanceToOrigin = cameraToTarget.Length;
            double tenPercentOfDist = 0.1 * distanceToOrigin;
            double zoomDelta = (delta > 0) ? tenPercentOfDist : -tenPercentOfDist;

            Vector3D lookDirection = camera.LookDirection;
            lookDirection.Normalize();  // Ensure it's a unit vector
            Point3D newPosition = camera.Position + lookDirection * zoomDelta;
            Vector3D newOriginVector = newPosition - origin;
            double minDistanceToOrigin = 0.5; // Adjust as necessary
            // Only zoom in so far
            if (newOriginVector.Length > minDistanceToOrigin)
            {
                camera.Position = newPosition;
            }
        }


        internal static void RotateCamera(PerspectiveCamera camera, DirectionalLight cameraLight, double deltaX, double deltaY)
        {
            if (camera == null || camera.IsFrozen) { return; }

            const double sensitivity = 0.005;
            // Calculate radians based on deltaX and deltaY
            double radiansX = deltaX * sensitivity;
            double radiansY = deltaY * sensitivity;

            Vector3D lookNormalized = camera.LookDirection;
            lookNormalized.Normalize();
            Vector3D cameraRightDirection = Vector3D.CrossProduct(lookNormalized, camera.UpDirection);
            cameraRightDirection.Normalize();
            Vector3D upNormalized = Vector3D.CrossProduct(cameraRightDirection, lookNormalized);
            upNormalized.Normalize();

            double degrees = radiansX * (180 / Math.PI);
            AxisAngleRotation3D axisAngleRotation = new AxisAngleRotation3D(upNormalized, degrees);
            RotateTransform3D rotateTransform = new RotateTransform3D(axisAngleRotation);
            Point3D newCameraPosition = rotateTransform.Transform(camera.Position);
            camera.Position = newCameraPosition;
            camera.LookDirection = new Point3D(0, 0, 0) - camera.Position;


            Vector3D rightDirectionNormalized = Vector3D.CrossProduct(lookNormalized, upNormalized);
            rightDirectionNormalized.Normalize();
            double degreesY = radiansY * (180 / Math.PI);

            AxisAngleRotation3D axisAngleRotationY = new AxisAngleRotation3D(rightDirectionNormalized, degreesY);
            RotateTransform3D rotateTransformY = new RotateTransform3D(axisAngleRotationY);
            Point3D newCameraPositionY = rotateTransformY.Transform(camera.Position);

            //Do not move camera if its nearing the poles
            double newAngle = CalculateAngleFromOrigin(newCameraPositionY);
            if (newAngle < 4.0 || newAngle > 176.0)
            {
                return;
            }

            camera.Position = newCameraPositionY;
            camera.LookDirection = new Point3D(0, 0, 0) - camera.Position;
            // Update the camera light
            cameraLight.Direction = camera.LookDirection;
        }

        internal static double CalculateAngleFromOrigin(Point3D newCameraPositionY)
        {
            Vector3D vectorToNewCameraPosition = new Vector3D(newCameraPositionY.X, newCameraPositionY.Y, newCameraPositionY.Z);

            // Unit vector along the Y-axis
            Vector3D unitY = new Vector3D(0, 1, 0);

            // Calculate the dot product between the two vectors
            double dotProduct = Vector3D.DotProduct(unitY, vectorToNewCameraPosition);

            // Calculate the magnitudes of the vectors
            double magnitudeY = unitY.Length;
            double magnitudeCamera = vectorToNewCameraPosition.Length;

            // Calculate the cosine of the angle
            double cosTheta = dotProduct / (magnitudeY * magnitudeCamera);

            // Use arccos to find the angle in radians and then convert to degrees
            double angleInRadians = Math.Acos(cosTheta);
            double angleInDegrees = angleInRadians * (180.0 / Math.PI);

            return angleInDegrees;
        }

    }
}
