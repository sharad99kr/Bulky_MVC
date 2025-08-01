﻿using Bulky.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository.IRepository
{
	public interface IOrderHeaderRepository : IRepository<OrderHeader>
	{
		//This interface will get all the methods defined in IRepository
		//and in addition, implement 2 new methods i.e. Update and Save
		void Update(OrderHeader orderHeader);
		void UpdateStatus(int id, string orderStatus, string? paymentStatus=null);
		//payment status can be null as it remains same most of the time while orderStatus changes frequently 
		void UpdateStripePaymentId(int id, string sessionId, string paymentIntentId);
	}
}
