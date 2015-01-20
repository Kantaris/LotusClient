package com.jn.webservice.api;

import java.io.PrintWriter;
import java.io.StringWriter;

import javax.servlet.http.HttpServletResponse;
import javax.xml.parsers.DocumentBuilder;
import javax.xml.parsers.DocumentBuilderFactory;
import javax.xml.transform.OutputKeys;
import javax.xml.transform.Transformer;
import javax.xml.transform.TransformerFactory;
import javax.xml.transform.dom.DOMSource;
import javax.xml.transform.stream.StreamResult;

import org.w3c.dom.Document;
import org.w3c.dom.Element;

public class Controller {

	public String makeXMLString(Document doc) {
		String xmlString = "";

		if (doc != null) {
			try {
				TransformerFactory transfac = TransformerFactory.newInstance();
				Transformer trans = transfac.newTransformer();
				trans.setOutputProperty(OutputKeys.OMIT_XML_DECLARATION, "yes");
				trans.setOutputProperty(OutputKeys.INDENT, "yes");
				StringWriter sw = new StringWriter();
				StreamResult result = new StreamResult(sw);
				DOMSource source = new DOMSource(doc);
				trans.transform(source, result);
				xmlString = sw.toString();
			} catch (Exception e) {
				e.printStackTrace();
			}
		}

		return xmlString;
	}

	public void print(HttpServletResponse res, String result) throws Exception {
		res.setContentType("text/xml;charset=UTF-8");
		try {
			PrintWriter pwOut = res.getWriter();
			pwOut.println("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
			pwOut.print(result);
			pwOut.flush();
			pwOut.close();
		} catch (Exception e) {
		}
	}
	
	public String getResponseElement(Constants.ResponseStatus error){
		StringBuffer sfXml = new StringBuffer();
		sfXml.append("<api version=\"" + Constants.Version + "\">")
				  .append("<response>")
				  .append("<code>" + error.code() +  "</code>")
				  .append("<msg>" + error.msg() + "</msg>")
				  .append("</response>")
				  .append("</api>");
		
		return sfXml.toString();
	}
	
	public Element getRootElement(Document doc){
		Element root = doc.createElement("api");
		root.setAttribute("version", Constants.Version);
		return root;
	}

	public Element getResponseElement(Document doc, Constants.ResponseStatus status){
		Element root = doc.createElement("response");
		
		Element code= doc.createElement("code");
		code.appendChild(doc.createTextNode(status.code()));
		root.appendChild(code);
		
		Element msg = doc.createElement("msg");
		msg.appendChild(doc.createTextNode(status.msg()));
		root.appendChild(msg);
		
		return root;
	}

	public Document getDocument() throws Exception {
		DocumentBuilderFactory icFactory = DocumentBuilderFactory.newInstance();
		DocumentBuilder icBuilder = icFactory.newDocumentBuilder();
		Document doc = icBuilder.newDocument();
		return doc;
	}
	
	public Element getResultElement(Document doc, Constants.APIType type){
		Element result = doc.createElement("result");
		result.setAttribute("type", type.type());
		return result;
	}
	
	public Element getSessionIDElement(Document doc, String sessionID){
		Element p = doc.createElement("session_id");
		p.appendChild(doc.createTextNode(sessionID));
		return p;
	}
	
	public Element getServerElement(Document doc, int count){
		Element result = doc.createElement("servers");
		result.setAttribute("count", String.valueOf(count));
		return result;
	}
	
	public Element getKeyElement(Document doc, String field, String key){
		Element p = doc.createElement(field);
		p.appendChild(doc.createTextNode(key));
		return p;
	}
}
