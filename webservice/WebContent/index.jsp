<%
	response.setContentType("text/xml; charset=UTF-8");
	response.getWriter()
			.append("<?xml version=\"1.0\" encoding=\"utf-8\"?>")
			.append("<api version=\"1.0\">").append("<response>")
			.append("<code>-3</code>").append("<msg>Bad Request</msg>")
			.append("</response>").append("</api>");
%>
