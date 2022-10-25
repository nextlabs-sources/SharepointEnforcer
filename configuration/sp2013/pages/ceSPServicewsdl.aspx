<%@ Page Language="C#" Inherits="System.Web.UI.Page"    %> <%@ Assembly Name="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Import Namespace="Microsoft.SharePoint.Utilities" %> <%@ Import Namespace="Microsoft.SharePoint" %>
<?xml version="1.0" encoding="utf-8"?>
<wsdl:definitions xmlns:soap="http://schemas.xmlsoap.org/wsdl/soap/" xmlns:tm="http://microsoft.com/wsdl/mime/textMatching/" xmlns:soapenc="http://schemas.xmlsoap.org/soap/encoding/" xmlns:mime="http://schemas.xmlsoap.org/wsdl/mime/" xmlns:tns="http://www.nextlabs.com/" xmlns:s1="http://microsoft.com/wsdl/types/" xmlns:s="http://www.w3.org/2001/XMLSchema" xmlns:soap12="http://schemas.xmlsoap.org/wsdl/soap12/" xmlns:http="http://schemas.xmlsoap.org/wsdl/http/" targetNamespace="http://www.nextlabs.com/" xmlns:wsdl="http://schemas.xmlsoap.org/wsdl/">
  <wsdl:types>
    <s:schema elementFormDefault="qualified" targetNamespace="http://www.nextlabs.com/">
      <s:import namespace="http://microsoft.com/wsdl/types/" />
      <s:element name="IsDocLib">
        <s:complexType>
          <s:sequence>
            <s:element minOccurs="0" maxOccurs="1" name="pathFolder" type="s:string" />
          </s:sequence>
        </s:complexType>
      </s:element>
      <s:element name="IsDocLibResponse">
        <s:complexType>
          <s:sequence>
            <s:element minOccurs="1" maxOccurs="1" name="IsDocLibResult" type="s:boolean" />
          </s:sequence>
        </s:complexType>
      </s:element>
      <s:element name="UploadDocument">
        <s:complexType>
          <s:sequence>
            <s:element minOccurs="0" maxOccurs="1" name="fileName" type="s:string" />
            <s:element minOccurs="0" maxOccurs="1" name="fileContents" type="s:base64Binary" />
            <s:element minOccurs="0" maxOccurs="1" name="pathFolder" type="s:string" />
            <s:element minOccurs="1" maxOccurs="1" name="ItemId" type="s:int" />
            <s:element minOccurs="0" maxOccurs="1" name="itemPath" type="s:string" />
            <s:element minOccurs="0" maxOccurs="1" name="webUrl" type="s:string" />
            <s:element minOccurs="1" maxOccurs="1" name="bIfUnique" type="s:boolean" />
          </s:sequence>
        </s:complexType>
      </s:element>
      <s:element name="UploadDocumentResponse">
        <s:complexType>
          <s:sequence>
            <s:element minOccurs="0" maxOccurs="1" name="UploadDocumentResult" type="s:string" />
            <s:element minOccurs="1" maxOccurs="1" name="ItemId" type="s:int" />
            <s:element minOccurs="0" maxOccurs="1" name="itemPath" type="s:string" />
            <s:element minOccurs="0" maxOccurs="1" name="webUrl" type="s:string" />
          </s:sequence>
        </s:complexType>
      </s:element>
      <s:element name="SetColumns">
        <s:complexType>
          <s:sequence>
            <s:element minOccurs="1" maxOccurs="1" name="id" type="s:int" />
            <s:element minOccurs="1" maxOccurs="1" name="guid" type="s1:guid" />
            <s:element minOccurs="0" maxOccurs="1" name="pathFolder" type="s:string" />
            <s:element minOccurs="0" maxOccurs="1" name="fieldName" type="s:string" />
            <s:element minOccurs="0" maxOccurs="1" name="fieldValue" type="s:string" />
          </s:sequence>
        </s:complexType>
      </s:element>
      <s:element name="SetColumnsResponse">
        <s:complexType>
          <s:sequence>
            <s:element minOccurs="0" maxOccurs="1" name="SetColumnsResult" type="s:string" />
          </s:sequence>
        </s:complexType>
      </s:element>
      <s:element name="IfInAssociationTemplates">
        <s:complexType>
          <s:sequence>
            <s:element minOccurs="0" maxOccurs="1" name="pathFolder" type="s:string" />
            <s:element minOccurs="0" maxOccurs="1" name="associationName" type="s:string" />
            <s:element minOccurs="0" maxOccurs="1" name="associationTemplates" type="tns:ArrayOfString" />
          </s:sequence>
        </s:complexType>
      </s:element>
      <s:complexType name="ArrayOfString">
        <s:sequence>
          <s:element minOccurs="0" maxOccurs="unbounded" name="string" nillable="true" type="s:string" />
        </s:sequence>
      </s:complexType>
      <s:element name="IfInAssociationTemplatesResponse">
        <s:complexType>
          <s:sequence>
            <s:element minOccurs="1" maxOccurs="1" name="IfInAssociationTemplatesResult" type="s:boolean" />
          </s:sequence>
        </s:complexType>
      </s:element>
      <s:element name="StartWorkFlow">
        <s:complexType>
          <s:sequence>
            <s:element minOccurs="1" maxOccurs="1" name="itemID" type="s:int" />
            <s:element minOccurs="0" maxOccurs="1" name="itemNamestring" type="s:string" />
            <s:element minOccurs="0" maxOccurs="1" name="pathFolder" type="s:string" />
            <s:element minOccurs="0" maxOccurs="1" name="associationName" type="s:string" />
            <s:element minOccurs="0" maxOccurs="1" name="associationDes" type="s:string" />
            <s:element minOccurs="1" maxOccurs="1" name="bIsAutoStart" type="s:boolean" />
            <s:element minOccurs="1" maxOccurs="1" name="bHasWorkFlowRunning" type="s:boolean" />
            <s:element minOccurs="1" maxOccurs="1" name="iWorkFlowStatus" type="s:int" />
          </s:sequence>
        </s:complexType>
      </s:element>
      <s:element name="StartWorkFlowResponse">
        <s:complexType>
          <s:sequence>
            <s:element minOccurs="0" maxOccurs="1" name="StartWorkFlowResult" type="s:string" />
            <s:element minOccurs="1" maxOccurs="1" name="bHasWorkFlowRunning" type="s:boolean" />
            <s:element minOccurs="1" maxOccurs="1" name="iWorkFlowStatus" type="s:int" />
          </s:sequence>
        </s:complexType>
      </s:element>
    </s:schema>
    <s:schema elementFormDefault="qualified" targetNamespace="http://microsoft.com/wsdl/types/">
      <s:simpleType name="guid">
        <s:restriction base="s:string">
          <s:pattern value="[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}" />
        </s:restriction>
      </s:simpleType>
    </s:schema>
  </wsdl:types>
  <wsdl:message name="IsDocLibSoapIn">
    <wsdl:part name="parameters" element="tns:IsDocLib" />
  </wsdl:message>
  <wsdl:message name="IsDocLibSoapOut">
    <wsdl:part name="parameters" element="tns:IsDocLibResponse" />
  </wsdl:message>
  <wsdl:message name="UploadDocumentSoapIn">
    <wsdl:part name="parameters" element="tns:UploadDocument" />
  </wsdl:message>
  <wsdl:message name="UploadDocumentSoapOut">
    <wsdl:part name="parameters" element="tns:UploadDocumentResponse" />
  </wsdl:message>
  <wsdl:message name="SetColumnsSoapIn">
    <wsdl:part name="parameters" element="tns:SetColumns" />
  </wsdl:message>
  <wsdl:message name="SetColumnsSoapOut">
    <wsdl:part name="parameters" element="tns:SetColumnsResponse" />
  </wsdl:message>
  <wsdl:message name="IfInAssociationTemplatesSoapIn">
    <wsdl:part name="parameters" element="tns:IfInAssociationTemplates" />
  </wsdl:message>
  <wsdl:message name="IfInAssociationTemplatesSoapOut">
    <wsdl:part name="parameters" element="tns:IfInAssociationTemplatesResponse" />
  </wsdl:message>
  <wsdl:message name="StartWorkFlowSoapIn">
    <wsdl:part name="parameters" element="tns:StartWorkFlow" />
  </wsdl:message>
  <wsdl:message name="StartWorkFlowSoapOut">
    <wsdl:part name="parameters" element="tns:StartWorkFlowResponse" />
  </wsdl:message>
  <wsdl:portType name="DocUploadSoap">
    <wsdl:operation name="IsDocLib">
      <wsdl:input message="tns:IsDocLibSoapIn" />
      <wsdl:output message="tns:IsDocLibSoapOut" />
    </wsdl:operation>
    <wsdl:operation name="UploadDocument">
      <wsdl:input message="tns:UploadDocumentSoapIn" />
      <wsdl:output message="tns:UploadDocumentSoapOut" />
    </wsdl:operation>
    <wsdl:operation name="SetColumns">
      <wsdl:input message="tns:SetColumnsSoapIn" />
      <wsdl:output message="tns:SetColumnsSoapOut" />
    </wsdl:operation>
    <wsdl:operation name="IfInAssociationTemplates">
      <wsdl:input message="tns:IfInAssociationTemplatesSoapIn" />
      <wsdl:output message="tns:IfInAssociationTemplatesSoapOut" />
    </wsdl:operation>
    <wsdl:operation name="StartWorkFlow">
      <wsdl:input message="tns:StartWorkFlowSoapIn" />
      <wsdl:output message="tns:StartWorkFlowSoapOut" />
    </wsdl:operation>
  </wsdl:portType>
  <wsdl:binding name="DocUploadSoap" type="tns:DocUploadSoap">
    <soap:binding transport="http://schemas.xmlsoap.org/soap/http" />
    <wsdl:operation name="IsDocLib">
      <soap:operation soapAction="http://www.nextlabs.com/IsDocLib" style="document" />
      <wsdl:input>
        <soap:body use="literal" />
      </wsdl:input>
      <wsdl:output>
        <soap:body use="literal" />
      </wsdl:output>
    </wsdl:operation>
    <wsdl:operation name="UploadDocument">
      <soap:operation soapAction="http://www.nextlabs.com/UploadDocument" style="document" />
      <wsdl:input>
        <soap:body use="literal" />
      </wsdl:input>
      <wsdl:output>
        <soap:body use="literal" />
      </wsdl:output>
    </wsdl:operation>
    <wsdl:operation name="SetColumns">
      <soap:operation soapAction="http://www.nextlabs.com/SetColumns" style="document" />
      <wsdl:input>
        <soap:body use="literal" />
      </wsdl:input>
      <wsdl:output>
        <soap:body use="literal" />
      </wsdl:output>
    </wsdl:operation>
    <wsdl:operation name="IfInAssociationTemplates">
      <soap:operation soapAction="http://www.nextlabs.com/IfInAssociationTemplates" style="document" />
      <wsdl:input>
        <soap:body use="literal" />
      </wsdl:input>
      <wsdl:output>
        <soap:body use="literal" />
      </wsdl:output>
    </wsdl:operation>
    <wsdl:operation name="StartWorkFlow">
      <soap:operation soapAction="http://www.nextlabs.com/StartWorkFlow" style="document" />
      <wsdl:input>
        <soap:body use="literal" />
      </wsdl:input>
      <wsdl:output>
        <soap:body use="literal" />
      </wsdl:output>
    </wsdl:operation>
  </wsdl:binding>
  <wsdl:binding name="DocUploadSoap12" type="tns:DocUploadSoap">
    <soap12:binding transport="http://schemas.xmlsoap.org/soap/http" />
    <wsdl:operation name="IsDocLib">
      <soap12:operation soapAction="http://www.nextlabs.com/IsDocLib" style="document" />
      <wsdl:input>
        <soap12:body use="literal" />
      </wsdl:input>
      <wsdl:output>
        <soap12:body use="literal" />
      </wsdl:output>
    </wsdl:operation>
    <wsdl:operation name="UploadDocument">
      <soap12:operation soapAction="http://www.nextlabs.com/UploadDocument" style="document" />
      <wsdl:input>
        <soap12:body use="literal" />
      </wsdl:input>
      <wsdl:output>
        <soap12:body use="literal" />
      </wsdl:output>
    </wsdl:operation>
    <wsdl:operation name="SetColumns">
      <soap12:operation soapAction="http://www.nextlabs.com/SetColumns" style="document" />
      <wsdl:input>
        <soap12:body use="literal" />
      </wsdl:input>
      <wsdl:output>
        <soap12:body use="literal" />
      </wsdl:output>
    </wsdl:operation>
    <wsdl:operation name="IfInAssociationTemplates">
      <soap12:operation soapAction="http://www.nextlabs.com/IfInAssociationTemplates" style="document" />
      <wsdl:input>
        <soap12:body use="literal" />
      </wsdl:input>
      <wsdl:output>
        <soap12:body use="literal" />
      </wsdl:output>
    </wsdl:operation>
    <wsdl:operation name="StartWorkFlow">
      <soap12:operation soapAction="http://www.nextlabs.com/StartWorkFlow" style="document" />
      <wsdl:input>
        <soap12:body use="literal" />
      </wsdl:input>
      <wsdl:output>
        <soap12:body use="literal" />
      </wsdl:output>
    </wsdl:operation>
  </wsdl:binding>
  <wsdl:service name="DocUpload">
    <wsdl:port name="DocUploadSoap" binding="tns:DocUploadSoap">
      <soap:address location=<% SPHttpUtility.AddQuote(SPHttpUtility.HtmlEncode(SPWeb.OriginalBaseUrl(Request)),Response.Output); %> />
    </wsdl:port>
    <wsdl:port name="DocUploadSoap12" binding="tns:DocUploadSoap12">
      <soap12:address location=<% SPHttpUtility.AddQuote(SPHttpUtility.HtmlEncode(SPWeb.OriginalBaseUrl(Request)),Response.Output); %> />
    </wsdl:port>
  </wsdl:service>
</wsdl:definitions>