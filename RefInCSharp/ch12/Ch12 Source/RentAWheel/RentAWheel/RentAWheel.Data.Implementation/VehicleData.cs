﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using RentAWheel.Business;

namespace RentAWheel.Data.Implementation
{
    public class VehicleData : AbstractData<Vehicle>
    {
        private const string selectAllFromBranchSql = "Select * from Branch";
        private const string selectAllFromModelSql = "Select * from Model";
        private const string deleteVehicleSql = "Delete From Vehicle Where LicensePlate = @LicensePlate";
        private const string insertVehicleSql = "Insert Into Vehicle (LicensePlate, ModelId,BranchId, Tank, Mileage) Values(@LicensePlate, @ModelId, @BranchId, @Tank, @Mileage)";
        private const string updateVehicleSql = "Update Vehicle Set ModelId = @ModelId, BranchId = @BranchId,  Available = @Available, Operational = @Operational, Mileage = @Mileage, Tank = @Tank, CustomerFirstName = @CustomerFirstName, CustomerLastName = @CustomerLastName, CustomerDocNumber = @CustomerDocNumber, CustomerDocType = @CustomerDocType Where LicensePlate = @LicensePlate";

        private const string licensePlateParameterName = "@LicensePlate";
        private const string modelIdParameterName = "@ModelId";
        private const string branchIdParameterName = "@BranchId";

        private const string availableParameterName = "@Available";
        private const string operationalParameterName = "@Operational";
        private const string mileageParameterName = "@Mileage";
        private const string tankParameterName = "@Tank";
        private const string customerFirstNameParameterName = "@CustomerFirstName";
        private const string customerLastNameParameterName = "@CustomerLastName";
        private const string customerDocNumberParameterName = "@CustomerDocNumber";
        private const string customerDocTypeParameterName = "@CustomerDocType";
        private const string categoryIdParameterName = "@CategoryId";

        private const string vehicleTableLicensePlateColumnName = "LicensePlate";
        private const string vehicleTableTankLevelColumnName = "Tank";
        private const string vehicleTableMileageColumnName = "Mileage";
        private const string vehicleTableRentalState  = "Available";
        private const string vehicleTableOperational = "Operational";

        private const string branchTableNameColumnName = "BranchName";
        private const string modelTableNameColumnName = "ModelName";
        private const string customerFirstName = "CustomerFirstName";
        private const string customerLastName = "CustomerLastName";
        private const string customerDocNumber = "CustomerDocNumber";
        private const string customerDocType = "CustomerDocType";

       private const string selectVehicleJoinBranchJoinModelJoinCategorySql = "Select * from Vehicle Inner Join Branch On Vehicle.BranchId = Branch.BranchId Inner Join Model On Vehicle.ModelId = Model.ModelId Inner Join Category on Model.CategoryId = Category.CategoryId";

        private const int singleTableInDatasetIndex = 0;

        public override IList<Vehicle> GetAll()
        {
            IDbCommand command = new SqlCommand();
            DataSet vehiclesSet = FillDataset(command, selectVehicleJoinBranchJoinModelJoinCategorySql);
            DataTable vehiclesTable = vehiclesSet.Tables[singleTableInDatasetIndex];
            return VehiclesFromTable(vehiclesTable);
        }

        private static IList<Vehicle> VehiclesFromTable(DataTable vehiclesTable)
        {
            IList<Vehicle> vehicles = new List<Vehicle>();
            foreach (DataRow row in vehiclesTable.Rows)
            {   
                Vehicle vehicle =  new Vehicle(
                            row[vehicleTableLicensePlateColumnName].ToString(),
                            ModelData.ModelFromRow(row),
                            BranchData.BranchFromRow(row),
                            Convert.IsDBNull(row[vehicleTableTankLevelColumnName]) ? 0 : (TankLevel)Convert.ToInt32(row[vehicleTableTankLevelColumnName]),
                            Convert.IsDBNull(row[vehicleTableMileageColumnName]) ? 0 : Convert.ToInt32(row[vehicleTableMileageColumnName]),
                            CustomerFromRow(row)
                            );
                vehicle.Operational = Convert.IsDBNull(row[vehicleTableOperational]) ? 0 : (Operational)Convert.ToInt32(row[vehicleTableOperational]);
                vehicle.RentalState = Convert.IsDBNull(row[vehicleTableRentalState]) ? 0 : (RentalState)Convert.ToInt32(row[vehicleTableRentalState]);
                vehicles.Add(vehicle);
            }
            return vehicles;
        }

        public override void Delete(Vehicle vehicle)
        {
            IDbCommand command = new SqlCommand();
            AddParameter(command, licensePlateParameterName,
                DbType.String, vehicle.LicensePlate);
            ExecuteNonQuery(command, deleteVehicleSql);        
        }

        public IList<Vehicle> GetByCriteria(Nullable<Operational> operational, Nullable<RentalState> rentalState, Nullable<Int32> branchId, Nullable<Int32> categoryId)
        {
            if ((operational == null & rentalState == null & branchId == null & categoryId == null))
            {
                return this.GetAll();
            }
            IDbCommand command = new SqlCommand();
            string sql = selectVehicleJoinBranchJoinModelJoinCategorySql + " WHERE ";
            if (rentalState != null)
            {
                sql += "Vehicle.Available = @Available And ";
                AddParameter(command, availableParameterName, DbType.Int32, rentalState);
            }
            if (branchId != null)
            {
                sql += "Vehicle.BranchId = @BranchId And ";
                AddParameter(command, branchIdParameterName, DbType.String, branchId);
            }
            if (categoryId != null)
            {
                sql += "Model.CategoryId = @CategoryId And ";
                AddParameter(command, categoryIdParameterName, DbType.Int32, categoryId);
            }
            if (operational != null)
            {
                sql += "Vehicle.Operational = @Operational And ";
                AddParameter(command, operationalParameterName, DbType.Int32, (int)operational);
            }
            sql = RemoveTrailing_AND_(sql);
            DataSet dataSet = FillDataset(command, sql);
            DataTable table = dataSet.Tables[0];
            IList<Vehicle> vehicles = VehiclesFromTable(table);
            return vehicles;
        }

        private static string RemoveTrailing_AND_(string sql)
        {
            return sql.Substring(0, sql.Length - 5);
        }

        private static Customer CustomerFromRow(DataRow row)
        {
            Customer customer = null;
            if (CustomerExist(row))
            {
                customer = new Customer();
                customer.FirstName = row[customerFirstName].ToString();
                customer.LastName = row[customerLastName].ToString();
                customer.DocType = row[customerDocType].ToString();
                customer.DocNumber = row[customerDocNumber].ToString();
            }
            return customer;
        }

        private static bool CustomerExist(DataRow row)
        {
            return !Convert.IsDBNull(row[customerDocNumber]);
        }

        public override void Update(Vehicle vehicle)
        {
            IDbCommand command = new SqlCommand();
            AddCommonParameters(vehicle, command);
            AddParameter(command, availableParameterName, DbType.Int32, vehicle.RentalState);
            AddParameter(command, operationalParameterName, DbType.Int32, vehicle.Operational);
            string firstName = string.Empty;
            string lastName = string.Empty;
            string docNumber = string.Empty;
            string docType = string.Empty;
            if ((vehicle.Customer != null))
            {
                firstName = vehicle.Customer.FirstName;
                lastName = vehicle.Customer.LastName;
                docNumber = vehicle.Customer.DocNumber;
                docType = vehicle.Customer.DocType;
            }
            AddParameter(command, customerFirstNameParameterName, DbType.String, firstName);
            AddParameter(command, customerLastNameParameterName, DbType.String, lastName);
            AddParameter(command, customerDocNumberParameterName, DbType.String, docNumber);
            AddParameter(command, customerDocTypeParameterName, DbType.String, docType);
            ExecuteNonQuery(command, updateVehicleSql);
        }

        private void AddCommonParameters(Vehicle vehicle, IDbCommand command)
        {
            AddParameter(command, licensePlateParameterName, DbType.String, vehicle.LicensePlate);
            AddParameter(command, modelIdParameterName, DbType.Int32, vehicle.Model.Id);
            AddParameter(command, branchIdParameterName, DbType.Int32, vehicle.Branch.Id);
            AddParameter(command, tankParameterName, DbType.Int32, vehicle.TankLevel);
            AddParameter(command, mileageParameterName, DbType.Int32, vehicle.Mileage);
        }

        public override void Insert(Vehicle vehicle)
        {
            IDbCommand command = new SqlCommand();
            AddCommonParameters(vehicle, command);
            ExecuteNonQuery(command, insertVehicleSql);
        }
    }
}
