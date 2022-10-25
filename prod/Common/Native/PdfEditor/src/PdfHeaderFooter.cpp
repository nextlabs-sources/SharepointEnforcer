// PdfHeaderFooter.cpp : Implementation of CPdfHeaderFooter

#include "podofo.h"
#include "stdafx.h"
#include "PdfHeaderFooter.h"
#include <iostream>
#include <fstream>
#include <string.h>
using namespace PoDoFo;
using std::wstring;
using std::ifstream;

// wingdi.h define "CreateFont" to "CreateFontW"
#ifdef CreateFont
#define CreateFont CreateFont
#endif

// CPdfHeaderFooter
STDMETHODIMP CPdfHeaderFooter::AddHeaderText(BSTR filePath, BSTR headerText, BSTR position, LONG left, LONG height)
{
	// TODO: Add your implementation code here
	AddText(filePath, headerText, position, left, height, true);
	return S_OK;
}


bool CPdfHeaderFooter::AddText(BSTR filePath, BSTR headerText, BSTR position, LONG left, LONG height, bool bHeader)
{
	// TODO: Add your implementation code here

	try 
	{
		// Read the file to buffer.
		ifstream file(filePath, ifstream::binary);
		file.seekg(0, file.end);
		LONG length = (LONG)file.tellg();
		file.seekg(0, file.beg);	
		char* buffer = new char[length];
		file.read(buffer,length);
		file.close();

		// Load document from buffer.
		PdfMemDocument document;
		document.Load(buffer, length);
		PdfPainter painter;
		PdfPage* pPage = NULL;  
		PdfFont* pFont = NULL;

		pFont = document.CreateFont( L"Arial" );
		pFont->SetFontSize( height );
		painter.SetFont( pFont );
		EPdfAlignment aligenment = ePdfAlignment_Left;
		wstring pos = position;
		if(pos == L"Right")
		{
			aligenment = ePdfAlignment_Right;
		}
		else if(pos == L"Center")
		{
			aligenment = ePdfAlignment_Center;
		}
		wstring wstrText = headerText;
		if(wstrText.length() > 255)
		{
			// limited the header text length to 255.
			wstrText = wstrText.substr(0, 255);
		}
		// the size of "rectHeight" equal (line number) * (font size) * 1.5
		double rectHeight =(wstrText.length() / 85 + 1) * height * 1.5;
		
		// Convert wstring to string to call "DrawMultiLineText" function.
		char* destBuf = new char[wstrText.length() + 1];
		WideCharToMultiByte(CP_ACP, 0, wstrText.c_str(), -1, destBuf, wstrText.length(), 0, 0);
		destBuf[wstrText.length()] = 0;
		
		int count = document.GetPageCount();
		for(int i = 0; i < count; i++)
		{
			pPage = document.GetPage(i);
			painter.SetPage( pPage );

			PdfRect pageSize = pPage->GetPageSize();
			double textLeft = pageSize.GetWidth() * left / 100;
			
			// fill the header region with white. 
			PdfRect rect(pageSize.GetLeft(), pageSize.GetHeight() - rectHeight, pageSize.GetWidth(), rectHeight );
			painter.SetColor(1.0, 1.0, 1.0);
			painter.FillRect(rect);

			// draw text in header region with black and pointed font.
			painter.SetColor(0.0, 0.0, 0.0);
			painter.DrawMultiLineText(pageSize.GetLeft() + textLeft, pageSize.GetHeight() - rectHeight, pageSize.GetWidth() - textLeft * 2, rectHeight, PdfString(destBuf), aligenment, ePdfVerticalAlignment_Center );
			painter.FinishPage();
		}
		document.Write(filePath);
		delete destBuf;
		delete[] buffer;
	} 
	catch ( ... ) 
	{
		return false;
	}
	return true;
}