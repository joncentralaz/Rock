﻿// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;

namespace RockWeb.Blocks.Core
{
    [DisplayName( "Category Tree View" )]
    [Category( "Core" )]
    [Description( "Displays a tree of categories for the configured entity type." )]

    [LinkedPage( "Detail Page" )]
    [EntityTypeField( "Entity Type", "Display categories associated with this type of entity" )]
    [TextField( "Entity Type Qualifier Property", "", false )]
    [TextField( "Entity type Qualifier Value", "", false )]
    [TextField( "Page Parameter Key", "The page parameter to look for" )]
    public partial class CategoryTreeView : RockBlock
    {
        /// <summary>
        /// The entity type name
        /// </summary>
        protected string RestParms;

        /// <summary>
        /// The page parameter name
        /// </summary>
        protected string PageParameterName;

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            RockPage.AddScriptLink( "~/Scripts/jquery.tinyscrollbar.js" );


            // Get EntityTypeName
            Guid entityTypeGuid = Guid.Empty;
            if ( Guid.TryParse( GetAttributeValue( "EntityType" ), out entityTypeGuid ) )
            {
                int entityTypeId = Rock.Web.Cache.EntityTypeCache.Read( entityTypeGuid ).Id;
                string entityTypeQualiferColumn = GetAttributeValue( "EntityTypeQualifierProperty" );
                string entityTypeQualifierValue = GetAttributeValue( "EntityTypeQualifierValue" );

                var parms = new StringBuilder();
                parms.AppendFormat( "/True/{0}", entityTypeId );
                if ( !string.IsNullOrEmpty( entityTypeQualiferColumn ) )
                {
                    parms.AppendFormat( "/{0}", entityTypeQualiferColumn );

                    if ( !string.IsNullOrEmpty( entityTypeQualifierValue ) )
                    {
                        parms.AppendFormat( "/{0}", entityTypeQualifierValue );
                    }
                }

                RestParms = parms.ToString();

                var cachedEntityType = Rock.Web.Cache.EntityTypeCache.Read( entityTypeId );
                if ( cachedEntityType != null )
                {
                    lbAddItem.ToolTip = "Add " + cachedEntityType.FriendlyName;
                    lAddItem.Text = cachedEntityType.FriendlyName;
                }

                PageParameterName = GetAttributeValue( "PageParameterKey" );
                string itemId = PageParameter( PageParameterName );
                string selectedEntityType = cachedEntityType.Name;
                if ( string.IsNullOrWhiteSpace( itemId ) )
                {
                    itemId = PageParameter( "categoryId" );
                    selectedEntityType = "category";
                }

                lbAddCategoryRoot.Enabled = true;
                lbAddCategoryChild.Enabled = false;
                lbAddItem.Enabled = false;

                if ( !string.IsNullOrWhiteSpace( itemId ) )
                {
                    hfInitialItemId.Value = itemId;
                    hfInitialEntityIsCategory.Value = ( selectedEntityType == "category" ).ToString();
                    hfSelectedCategoryId.Value = itemId;
                    List<string> parentIdList = new List<string>();

                    Category category = null;
                    if ( selectedEntityType.Equals( "category" ) )
                    {
                        category = new CategoryService().Get( int.Parse( itemId ) );
                        lbAddItem.Enabled = true;
                        lbAddCategoryChild.Enabled = true;
                    }
                    else
                    {
                        int id = 0;
                        if ( int.TryParse( itemId, out id ) )
                        {
                            if ( cachedEntityType != null )
                            {
                                Type entityType = cachedEntityType.GetEntityType();
                                if ( entityType != null )
                                {
                                    Type serviceType = typeof( Rock.Data.Service<> );
                                    Type[] modelType = { entityType };
                                    Type service = serviceType.MakeGenericType( modelType );
                                    var serviceInstance = Activator.CreateInstance( service );
                                    var getMethod = service.GetMethod( "Get", new Type[] { typeof( int ) } );
                                    ICategorized entity = getMethod.Invoke( serviceInstance, new object[] { id } ) as ICategorized;

                                    if ( entity != null )
                                    {
                                        lbAddCategoryRoot.Enabled = false;
                                        lbAddCategoryChild.Enabled = false;
                                        category = entity.Category;
                                        if ( category != null )
                                        {
                                            parentIdList.Insert( 0, category.Id.ToString() );
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // get the parents of the selected item so we can tell the treeview to expand those
                    while ( category != null )
                    {
                        category = category.ParentCategory;
                        if ( category != null )
                        {
                            parentIdList.Insert( 0, category.Id.ToString() );
                        }

                    }
                    // also get any additional expanded nodes that were sent in the Post
                    string postedExpandedIds = this.Request.Params["expandedIds"];
                    if ( !string.IsNullOrWhiteSpace( postedExpandedIds ) )
                    {
                        var postedExpandedIdList = postedExpandedIds.Split( ',' ).ToList();
                        foreach ( var id in postedExpandedIdList )
                        {
                            if ( !parentIdList.Contains( id ) )
                            {
                                parentIdList.Add( id );
                            }
                        }
                    }

                    hfInitialCategoryParentIds.Value = parentIdList.AsDelimited( "," );
                }
            }
        }


        /// <summary>
        /// Handles the Click event of the lbAddItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void lbAddItem_Click( object sender, EventArgs e )
        {
            int parentCategoryId = 0;
            if ( Int32.TryParse( hfSelectedCategoryId.Value, out parentCategoryId ) )
            {
                NavigateToLinkedPage( "DetailPage", PageParameterName, 0, "parentCategoryId", parentCategoryId );
            }
        }
        protected void lbAddCategoryChild_Click(object sender, EventArgs e)
        {
            int parentCategoryId = 0;
            if (Int32.TryParse(hfSelectedCategoryId.Value, out parentCategoryId))
            {
                NavigateToLinkedPage("DetailPage", "CategoryId", 0, "parentCategoryId", parentCategoryId);
            }
            else
            {
                NavigateToLinkedPage("DetailPage", "CategoryId", 0);
            }
        }
        protected void lbAddCategoryRoot_Click(object sender, EventArgs e)
        {
            NavigateToLinkedPage("DetailPage", "CategoryId", 0);
        }
}
}