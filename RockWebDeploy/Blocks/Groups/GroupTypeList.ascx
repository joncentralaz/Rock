﻿<%@ control language="C#" autoeventwireup="true" inherits="RockWeb.Blocks.Groups.GroupTypeList, RockWeb" %>

<asp:UpdatePanel ID="upGroupType" runat="server">
    <ContentTemplate>
        
        <div class="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-sitemap"></i> Group Type List</h1>
            </div>
            <div class="panel-body">

                <div class="grid grid-panel">
                    <Rock:GridFilter ID="rFilter" runat="server" OnDisplayFilterValue="rFilter_DisplayFilterValue">
                        <Rock:RockDropDownList ID="ddlPurpose" runat="server" Label="Purpose"></Rock:RockDropDownList>
                        <Rock:RockDropDownList ID="ddlIsSystem" runat="server" Label="System Group Type">
                            <asp:ListItem Value="" Text=" " />
                            <asp:ListItem Value="Yes" Text="Yes" />
                            <asp:ListItem Value="No" Text="No" />
                        </Rock:RockDropDownList>
                    </Rock:GridFilter>
                    <Rock:ModalAlert ID="mdGridWarning" runat="server" />
                    <Rock:Grid ID="gGroupType" runat="server" RowItemText="Group Type" OnRowSelected="gGroupType_Edit" TooltipField="Description">
                        <Columns>
                            <Rock:ReorderField />
                            <asp:BoundField DataField="Name" HeaderText="Name" SortExpression="Name" />
                            <asp:BoundField DataField="Purpose" HeaderText="Purpose" SortExpression="Purpose" />
                            <asp:BoundField DataField="GroupsCount" HeaderText="Group Count" SortExpression="GroupsCount" ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right" />
                            <Rock:BoolField DataField="IsSystem" HeaderText="System" SortExpression="IsSystem" />
                            <Rock:SecurityField TitleField="Name" />
                            <Rock:DeleteField OnClick="gGroupType_Delete" />
                        </Columns>
                    </Rock:Grid>
                </div>

            </div>
        </div>

        

    </ContentTemplate>
</asp:UpdatePanel>