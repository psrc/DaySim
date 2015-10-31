﻿// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

using Daysim.DomainModels.LD.Models.Interfaces;
using Daysim.DomainModels.LD.Wrappers.Interfaces;
using Daysim.Framework.Core;
using Daysim.Framework.DomainModels.Models;
using Daysim.Framework.Factories;

namespace Daysim.DomainModels.LD.Wrappers {
	[Factory(Factory.WrapperFactory, Category = Category.Wrapper, DataType = DataType.LD)]
	public class ParkAndRideNodeWrapper : Default.Wrappers.ParkAndRideNodeWrapper, ILDParkAndRideNodeWrapper {
		private readonly ILDParkAndRideNode _parkAndRideNode;

		[UsedImplicitly]
		public ParkAndRideNodeWrapper(IParkAndRideNode parkAndRideNode) : base(parkAndRideNode) {
			_parkAndRideNode = (ILDParkAndRideNode) parkAndRideNode;
		}
	}
}