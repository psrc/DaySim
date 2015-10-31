﻿// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

using Daysim.ChoiceModels;
using Daysim.Framework.Core;
using Daysim.Framework.DomainModels.Models;
using Daysim.Framework.Factories;

namespace Daysim.DomainModels.Persisters {
	[UsedImplicitly]
	[Factory(Factory.PersistenceFactory, Category = Category.Persister)]
	public class PersonDayPersister<TModel> : Persister<TModel> where TModel : class, IPersonDay, new() {
		public override void Export(IModel model) {
			base.Export(model);

			ChoiceModelFactory.PersonDayFileRecordsWritten++;

			var m = (TModel) model;

			ChoiceModelFactory.PersonDayHomeBasedToursCheckSum += m.HomeBasedTours;
			ChoiceModelFactory.PersonDayWorkBasedToursCheckSum += m.WorkBasedTours;
		}
	}
}